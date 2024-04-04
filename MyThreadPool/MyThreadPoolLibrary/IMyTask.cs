namespace MyThreadPoolLibrary;

public interface IMyTask<TResult>
{
    public bool IsCompleted { get;  }
    
    public TResult Result { get;  }

    public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuation);
    
    public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<IMyTask<TResult>, TNewResult> continuation);

}