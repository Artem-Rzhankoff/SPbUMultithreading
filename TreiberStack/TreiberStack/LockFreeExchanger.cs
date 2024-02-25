using static System.Threading.Interlocked;

namespace TreiberStack;

public class StampedReference <T>
{
    public T? Item = default;
    public int Stamp = 1;
    public StampedReference() {}

    public StampedReference(T? data, int stamp)
    {
        Item = data;
        Stamp = stamp;
    }
}

public class LockFreeExchanger<T>
{
    public StampedReference<T> Slot = new();
    private const int EMPTY = 1;
    private const int WAITING = 2;
    private const int BUSY = 3;
    
    // AtomicStampedReference<T> slot = new AtomicStampedReference<T>(null, 0);

    public T? MyExchange(T? myItem, long timeout)
    {
        var timeBound = DateTime.Now.Nanosecond + timeout;
        int[] stampHolder = [EMPTY]; // тут не ссылка ли должна быть ??
        while (true)
        {
            if (DateTime.Now.Nanosecond > timeBound)
            {
                throw new TimeoutException();
            }

            T yrItem = Slot.Item;
            int stamp = stampHolder[0];
            switch (stamp)
            {
                case EMPTY:
                    var newStampedReference = new StampedReference<T>(myItem, WAITING);
                    // 1. Add 1st value to slot
                    // 2. Set its stamp as WAITING (for 2nd)
                    if (CompareExchange(ref Slot, new StampedReference<T>(default, EMPTY),
                            newStampedReference) == newStampedReference)
                    {
                        while (DateTime.Now.Nanosecond < timeBound)
                        {
                            var dateItem = Slot.Item;
                            if (stampHolder[0] == BUSY)
                            {
                                // ожидающий поток -- единственный, кто может изменить состояние на empty
                                Exchange(ref Slot, new StampedReference<T>(default, EMPTY));
                                return dateItem;
                            }
                        }

                        var foo = new StampedReference<T>(default, EMPTY);

                        if (CompareExchange(ref Slot, newStampedReference, foo) == foo)
                        {
                            throw new TimeoutException();
                        }
                        else
                        {
                            // это значит, что другой поток перевел состояние WAITING в BUSY
                            var dateItem = Slot.Item;
                            Exchange(ref Slot, new StampedReference<T>(default, EMPTY));
                            return dateItem;
                        }
                    }
                    break;
                case WAITING:
                    var newReference = new StampedReference<T>(myItem, BUSY);
                    if (CompareExchange(ref Slot, new StampedReference<T>(yrItem, WAITING), newReference) ==
                        newReference)
                    {
                        return yrItem;
                    }

                    break;
                case BUSY:
                    break;
            }
        }
        
    }

}
