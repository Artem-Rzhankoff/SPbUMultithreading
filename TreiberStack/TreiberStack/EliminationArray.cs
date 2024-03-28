namespace TreiberStack;

public class EliminationArray<T> where T : class
{
    private readonly long _duration;
    private readonly LockFreeExchanger<T>[] _exchanger;
    private readonly Random _random;

    public EliminationArray(int capacity, long timeout )
    {
        _exchanger = new LockFreeExchanger<T> [capacity];
        for (var i = 0; i < capacity; i++)
        {
            _exchanger[i] = new LockFreeExchanger<T>();
        }

        _random = new Random();
        _duration =  timeout * 1_000_000; // convert milliseconds to nanoseconds
    }

    public T? Visit(T? value, int range)
    {
        var slot = _random.Next(range);
        return _exchanger[slot].MyExchange(value, _duration);
    }
}