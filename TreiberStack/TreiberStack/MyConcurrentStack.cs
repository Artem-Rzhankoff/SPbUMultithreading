using static System.Threading.Interlocked;

namespace TreiberStack;

public class MyConcurrentStack<T>
{

    private Node<T> _head;

    private static bool CompareAndSet(ref Node<T> value, Node<T> expectedValue, Node<T> newValue)
    {
        if (EqualityComparer<T>.Default.Equals(value.Data, expectedValue.Data))
        {
            value = newValue;
            return true;
        }

        return false;
    }
    
    public T Pop()
    {
        while (true)
        {
            var localHead = _head;
            if (CompareAndSet(ref _head, localHead, localHead.Next))
            {
                return localHead.Data;
            }
            
        }
    }

    public void Push(T value)
    {
        while (true)
        {
            var localHead = _head;
            var newNode = new Node<T>(value, localHead);
            if (CompareAndSet(ref _head, localHead, newNode))
            {
                return;
            }
        }
    }

    public T TopElement()
    {
        var localHead = _head;
        if (localHead == null)
        {
            throw new NullReferenceException();
        }

        return localHead.Data;
    }
}