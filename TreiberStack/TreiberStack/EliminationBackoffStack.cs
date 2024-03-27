namespace TreiberStack;

public class EliminationBackoffStack<T> : MyConcurrentStack<T> where T : class
{
    private const int Capacity = 100;
    private EliminationArray<T> _eliminationArray = new (Capacity, 10);

    public override void Push(T value)
    {
        var newNode = new Node<T>(value);
        
        while (true)
        {
            if (TryPush(newNode))
            {
                return;
            }

            try
            {
                var otherValue = _eliminationArray.Visit(value, 10);
                // гарантирует, что обмен был осуществлен с pop
                if (otherValue == null)
                {
                    return;
                }
            }
            catch (TimeoutException) { }
        }
    }

    public override T Pop()
    {
        while (true)
        {
            var returnNode = TryPop();
            if (returnNode != null)
            {
                return returnNode.Data;
            }

            try
            {
                var otherValue = _eliminationArray.Visit(null, 10);
                if (otherValue != null)
                {
                    return otherValue;
                }
            }
            catch (TimeoutException) { }
        }
    }
}