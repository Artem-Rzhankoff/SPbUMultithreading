using static System.Threading.Interlocked;

namespace TreiberStack;

public class MyConcurrentStack<T>
{

    private Node<T>? _head;
    
    public virtual void Push(T value)
    {
        var newNode = new Node<T>(value);
        while (true)
        {
            if (TryPush(newNode))
            {
                return;
            }
        }
    }
    
    public virtual T Pop()
    {
        while (true)
        {
            var returnNode = TryPop();
            if (returnNode != null)
            {
                return returnNode.Data;
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


    protected bool TryPush(Node<T> node)
    {
        var oldHead = _head;
        node.Next = oldHead;
        return CompareExchange(ref _head, oldHead, node) == node;
    }

    protected Node<T>? TryPop()
    {
        var oldNode = _head;
        if (oldNode == null)
        {
            throw new NullReferenceException();
        }

        var newNode = oldNode.Next;
        return CompareExchange(ref _head, oldNode, newNode) == newNode ? oldNode : null;
    }
    
}