using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using static System.Threading.Interlocked;
using static System.Console;

namespace TreiberStack;

public class LockFreeExchanger<T> where T : class
{
    private enum Stamp
    {
        Empty,
        WaitingPopper,
        WaitingPusher,
        Busy
    }
    
    private Tuple<T?, Stamp> _slot = new(null, Stamp.Empty);

    public T? MyExchange(T? myItem, long timeout)
    {
        var status = myItem == null ? Stamp.WaitingPopper : Stamp.WaitingPusher;
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        while (true)
        {
            if (stopwatch.ElapsedMilliseconds * 1000000 > timeout)
            {
                throw new TimeoutException();
            }
            
            var oldStamp = _slot;
            
            // компилятор гарантирует, что эта операция выполнится атомарно
            var (yrItem, stampHolder) = (_slot.Item1, _slot.Item2);
            
            switch (stampHolder)
            {
                case Stamp.Empty:
                    var newStamp = new Tuple<T?, Stamp>(myItem, status);
                    if (CompareExchange(ref _slot, newStamp,  oldStamp).Equals(oldStamp))
                    {
                        while (stopwatch.ElapsedMilliseconds * 10000000 < timeout)
                        {
                            yrItem = _slot.Item1;
                            // происходит обмен в данный момент
                            if (_slot.Item2 == Stamp.Busy) // под вопросом
                            {
                                // ожидающий поток -- единственный, кто может изменить состояние на empty
                                Exchange(ref _slot, new Tuple<T?, Stamp>(null, Stamp.Empty));
                                return yrItem;
                            }
                        }

                        var stampAfterExchange = new Tuple<T?, Stamp>(null, Stamp.Empty);

                        if (CompareExchange(ref _slot, stampAfterExchange, newStamp).Equals(newStamp))
                        {
                            throw new TimeoutException();
                        }
                        
                        // это значит, что другой поток перевел состояние WAITING в BUSY
                        yrItem = _slot.Item1;
                        Exchange(ref _slot, new Tuple<T?, Stamp>(null, Stamp.Empty)); 
                        return yrItem;

                    }
                    break;
                case Stamp.WaitingPopper:
                    var cellWithBusyStamp = new Tuple<T?, Stamp>(myItem, Stamp.Busy);
                    if (status != Stamp.WaitingPopper && CompareExchange(ref _slot, cellWithBusyStamp, oldStamp).Equals(oldStamp))
                    {
                        return yrItem;
                    }
                    break;
                case Stamp.WaitingPusher:
                    cellWithBusyStamp = new Tuple<T?, Stamp>(myItem, Stamp.Busy);
                    if (status != Stamp.WaitingPusher && CompareExchange(ref _slot, cellWithBusyStamp, oldStamp).Equals(oldStamp))
                    {
                        return yrItem;
                    }
                    break;
                case Stamp.Busy:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
