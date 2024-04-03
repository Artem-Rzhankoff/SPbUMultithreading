namespace MyThreadPool;

public class MyTask<TResult>(Func<TResult> function, ITaskScheduler scheduler, CancellationToken cancellationToken) : IMyTask<TResult>
{
    private enum State
    {
        NotCompleted,
        Completed
    }
    
    private TResult? _result;
    private Exception? _thrownException;
    private List<Action> _callbacks = [];
    private readonly ManualResetEvent _taskCompletionHandle = new(false);
    private int _convertedIsCompleted = (int)State.NotCompleted; // for using in CompareExchange
    

    public bool IsCompleted => _convertedIsCompleted == (int)State.Completed;
    
    public TResult Result
    {
        get
        {
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
            var currentCallbacks = _callbacks;
            cancellationToken.ThrowIfCancellationRequested();
            if (IsCompleted)
            {
                throw new InvalidOperationException($"Task is already completed with result = {Result}");
            }
            var continuationTask = new MyTask<TNewResult>(() => continuationAction(Result), scheduler, cancellationToken);
            var newCallbacks = _callbacks.Concat(new[] { () => continuationTask.Execute() }).ToList(); // Action --> Func<TNewResult> ?
            
            if (Interlocked.CompareExchange(ref _callbacks, newCallbacks, currentCallbacks) == currentCallbacks)
            {
                return continuationTask;
            }
        }
    }
    
    internal void Execute()
    {
        while (true)
        {
            var isCompletedState = _convertedIsCompleted;

            if (IsCompleted)
            {
                throw new InvalidOperationException($"Task is already completed with result = {Result}");
            }
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _result = function.Invoke();
            } catch (Exception ex)
            {
                _thrownException = ex;
            }

            if (Interlocked.CompareExchange(ref _convertedIsCompleted, 
                    (int)State.Completed, isCompletedState) == isCompletedState)
            {
                _callbacks.ForEach(scheduler.Enqueue);
                _taskCompletionHandle.Set();
            }
            
        }
    }
}