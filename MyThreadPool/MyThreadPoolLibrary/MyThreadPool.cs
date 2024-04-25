using System.Collections.Concurrent;

namespace MyThreadPoolLibrary;

public class MyThreadPool : ITaskScheduler, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ManualResetEvent _threadsRunHandle;
    private readonly List<Thread> _threads;
    private readonly ConcurrentQueue<Action> _tasks;
    private readonly object _lockObject;

    public MyThreadPool(int threadsCount)
    {
        if (threadsCount <= 0)
        {
            throw new ArgumentException($"Number of threads should be only positive, but actually is {threadsCount}");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _threadsRunHandle = new ManualResetEvent(false);
        _tasks = new ConcurrentQueue<Action>();
        _threads = new List<Thread>();
        _lockObject = new object();

        for (var i = 0; i < threadsCount; ++i)
        {
            var thread = new Thread(RunJob);
            thread.Start();
            _threads.Add(thread);
            
        }
    }
    
    public IMyTask<TResult> Enqueue<TResult>(Func<TResult> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        var task = new MyTask<TResult>(function, this, _cancellationTokenSource.Token);
        Enqueue(task.Execute);

        return task;
    }

    public void Enqueue(Action task)
    {
        ArgumentNullException.ThrowIfNull(task);

        lock (_lockObject)
        {
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            _tasks.Enqueue(task);
        }

        _threadsRunHandle.Set();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        foreach (var thread in _threads)
        {
            thread.Join();
        }
    }

    private void RunJob()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            _threadsRunHandle.WaitOne();
            
            if (_tasks.TryDequeue(out var task))
            {
                task.Invoke();
            }
            else
            {
                _threadsRunHandle.Reset();
            }
        }
    }
}
