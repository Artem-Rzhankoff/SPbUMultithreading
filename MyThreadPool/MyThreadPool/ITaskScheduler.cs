namespace MyThreadPool;

public interface ITaskScheduler
{
    public void Enqueue(Action task);
}