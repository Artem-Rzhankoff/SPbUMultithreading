namespace TreiberStack;

public class EliminationBackoffStack<T> : MyConcurrentStack<T> where T : class
{
    private const int Capacity = 100;
    private readonly EliminationArray<T> _eliminationArray = new (Capacity, 3);

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
                
                if (otherValue == null) // значит, что обмен был осуществлен с pop
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
                if (otherValue != null) // значит что обменялись с push
                {
                    return otherValue;
                }
            }
            catch (TimeoutException) { }
        }
    }
}