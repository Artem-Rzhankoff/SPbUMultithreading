namespace MyThreadPoolLibrary;

public interface ITaskScheduler
{
    public void Enqueue(Action task);
}