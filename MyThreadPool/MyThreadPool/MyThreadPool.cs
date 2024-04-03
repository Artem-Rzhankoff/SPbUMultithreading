using System.Collections.Concurrent;

namespace MyThreadPool;

public class MyThreadPool : ITaskScheduler, IDisposable
{
    private readonly int _threadsCount;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ManualResetEvent _threadsRunHandle;
    private List<Thread> _threads;
    private ConcurrentQueue<Action> _tasks;
    private readonly object _lockObject;

    public MyThreadPool(int threadsCount)
    {
        if (threadsCount <= 0)
        {
            throw new ArgumentException($"Number of threads should be only positive, but actually is {threadsCount}");
        }
        _threadsCount = threadsCount;
        _cancellationTokenSource = new CancellationTokenSource();
        _threadsRunHandle = new ManualResetEvent(false);
        _tasks = new ConcurrentQueue<Action>();
        _threads = new List<Thread>();
        _lockObject = new object();

        for (var i = 0; i < _threadsCount; ++i)
        {
            var thread = new Thread(() => RunJob());
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
            _tasks.Enqueue(task);
        }

        _threadsRunHandle.Set();
    }

    public void Dispose()
    {
        
    }

    private void RunJob()
    {
        // сюда вошел какой то поток
        while (_cancellationTokenSource.Token.IsCancellationRequested)
        {
            _threadsRunHandle.WaitOne();

            if (_tasks.TryDequeue(out var task)) // && !cancellationToken.IsCancellationRequested кажется что тут проверка необязательна
            {
                task.Invoke();
            }
        }
    }
}