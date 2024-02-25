namespace TreiberStack;

public class EliminationArray<T>
{
    private long _duration;
    private LockFreeExchanger<T>[] _exchanger;
    private Random _random;

    public EliminationArray(int capacity, long timeout )
    {
        _exchanger = new LockFreeExchanger<T> [capacity];
        for (int i = 0; i < capacity; i++)
        {
            _exchanger[i] = new LockFreeExchanger<T>();
        }

        _random = new Random();
        _duration =  timeout * 1000000; // convert milliseconds to nanoseconds
    }

    public T? Visit(T? value, int range)
    {
        var slot = _random.Next(range);
        return _exchanger[slot].MyExchange(value, _duration);
    }
}