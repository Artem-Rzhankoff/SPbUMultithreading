using static System.Threading.Interlocked;

namespace TreiberStack;

// обменник для двух потоков
public enum Exchanger
{
    Empty, // еще не использовался
    Waiting, // тут для данных, с которыми мы хотим поменяться, ждем другую операцию
    Busy // уже дождались, просто операция обмена еще не завершилась
}

// надо будет отнаследоваться от чего то, где будет определен CompareAndSet или найти его в стандартной библиотеке
public class StackWithEliminationOptimization<T>
{
    private Exchanger[] _eliminationArray;
    /*
     * как я пон обычно при неудачном CAS мы просто ждем какое то время и пытаемся еще раз, но тут вместо этого
     * мы будем искать в массиве что-то
     */

    void LesOperation(ThreadInfo<T> p)
    {
        while (true)
        {
            var randomThreadId = Random.Shared.Next(0, _eliminationArray.Length);
            switch (_eliminationArray[randomThreadId])
            {
                case Exchanger.Empty:
                    break;
                case Exchanger.Waiting:
                    break;
                default:
                    return;
            }
        }
    }
}

public class ThreadInfo<T>
{
    public int ThreadId;

    public enum OperationType1
    {
        Push,
        Pop,
        Top
    }

    public OperationType1 OperationType;

    public Node<T>? HoldNode;

    public int SpinTime; // время, на которое поток должен задержаться в ожидании столкновения
}