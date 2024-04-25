namespace MyThreadPoolLibrary;

public class MyTask<TResult>(Func<TResult> function, ITaskScheduler scheduler, CancellationToken cancellationToken) : IMyTask<TResult>
{
    private abstract record State;
    private record UncompletedState(List<Action> Callbacks) : State;
    private record CompletedState : State;

    private State _state = new UncompletedState([]);
    private TResult? _result;
    private Exception? _thrownException;
    private readonly ManualResetEvent _taskCompletionHandle = new(false);

    public bool IsCompleted => _state is CompletedState;
    
    public TResult Result
    {
        get
        {
            if (!IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            _taskCompletionHandle.WaitOne();
            
            if (_thrownException is not null)
            {
                throw new AggregateException(_thrownException);
            }

            return _result ?? throw new InvalidOperationException();
        }
    }
    
    public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuationAction)
    {
        ArgumentNullException.ThrowIfNull(continuationAction);
        while (true)
        {
            var currentState = _state;
            cancellationToken.ThrowIfCancellationRequested();
            switch (_state)
            {
                case CompletedState:
                {
                    throw new InvalidOperationException($"Task is already completed with result = {Result}");
                }
                case UncompletedState uncompletedState:
                {
                    var continuationTask = new MyTask<TNewResult>(() => continuationAction(Result), scheduler, cancellationToken);
                    var newState = new UncompletedState(uncompletedState.Callbacks.Concat(new[] { () => continuationTask.Execute() }).ToList());
            
                    if (Interlocked.CompareExchange(ref _state, newState, currentState) == currentState)
                    {
                        return continuationTask;
                    }

                    break;
                }
            }
        }
    }

    public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<IMyTask<TResult>, TNewResult> continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        
        while (true)
        {
            var currentState = _state;
            cancellationToken.ThrowIfCancellationRequested();

            switch (_state)
            {
                case CompletedState:
                {
                    throw new InvalidOperationException($"Task is already completed with result = {Result}");
                }
                case UncompletedState uncompletedState:
                {
                    var continuationTask = new MyTask<TNewResult>(() => continuation(this), scheduler, cancellationToken);
                    var newState = new UncompletedState(
                        uncompletedState.Callbacks.Concat(new[] { () => continuationTask.Execute() }).ToList());
            
                    if (Interlocked.CompareExchange(ref _state, newState, currentState) == currentState)
                    {
                        return continuationTask;
                    }

                    break;
                }
            }
        }
    }
    
    internal void  Execute()
    {
        while (true)
        {
            var currentState = _state;
            switch (_state)
            {
                case CompletedState:
                {
                    throw new InvalidOperationException($"Task is already completed with result = {Result}");
                }
                case UncompletedState uncompletedState:
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        _result = function.Invoke();
                    } catch (Exception ex)
                    {
                        _thrownException = ex;
                    }

                    var completedState = new CompletedState();

                    if (Interlocked.CompareExchange(ref _state, completedState, currentState) == currentState)
                    {
                        uncompletedState.Callbacks.ForEach(scheduler.Enqueue);
                        _taskCompletionHandle.Set();
                        return;
                    }
                    break;
                }
            }
        }
    }
}
