using System.Diagnostics;
using static System.Threading.Interlocked;
using static System.Console;

namespace TreiberStack;

public class LockFreeExchanger<T> where T : class
{
    private const int Empty = 0, WaitingPopper = 1, WaitingPusher = 2, Busy = 3;
    
    private Tuple<T?, int > _slot = new (null, Empty);

    public T? MyExchange(T? myItem, long timeout)
    {
        var status = myItem == null ? WaitingPopper : WaitingPusher;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        int[] stampHolder = [Empty];
        
        while (true)
        {
            if (stopwatch.ElapsedMilliseconds * 1000000 > timeout)
            {
                //WriteLine("first if been called");
                throw new TimeoutException();
            }

            //var yrItem = _slot.Reference;
            var oldStamp = _slot;
            var yrItem = _slot.Item1;
            stampHolder[0] = _slot.Item2;
            
            var stamp = stampHolder[0];
            
            
            switch (stamp)
            {
                case Empty:
                    var newStamp = new Tuple<T?, int>(myItem, status);
                    if (CompareExchange(ref _slot, newStamp,  oldStamp).Equals(oldStamp))
                    {
                        while (stopwatch.ElapsedMilliseconds * 10000000 < timeout)
                        {
                            yrItem = _slot.Item1;
                            if (stampHolder[0] == Busy)
                            {
                                // ожидающий поток -- единственный, кто может изменить состояние на empty
                                Exchange(ref _slot, new Tuple<T?, int>(null, Empty));
                                return yrItem;
                            }
                        }

                        var oldStamp1 = new Tuple<T?, int>(null, Empty);

                        if (CompareExchange(ref _slot, oldStamp1, newStamp).Equals(newStamp))
                        {
                            //WriteLine("112");
                            throw new TimeoutException();
                        }
                        
                        // это значит, что другой поток перевел состояние WAITING в BUSY
                        yrItem = _slot.Item1;
                        Exchange(ref _slot, new Tuple<T?, int>(null, Empty)); 
                        return yrItem;

                    }
                    break;
                case WaitingPopper:
                    //WriteLine("1");
                    var foo3 = new Tuple<T?, int>(myItem, Busy);
                    if (status != WaitingPopper && CompareExchange(ref _slot, foo3, oldStamp).Equals(oldStamp))
                    {
                        return yrItem;
                    }
                    break;
                case WaitingPusher:
                    //WriteLine("2");
                    foo3 = new Tuple<T?, int>(myItem, Busy);
                    if (status != WaitingPusher && CompareExchange(ref _slot, foo3, oldStamp).Equals(oldStamp))
                    {
                        return yrItem;
                    }
                    break;
                case Busy:
                    //WriteLine("3");
                    break;
            }
        }
        
    }

}
