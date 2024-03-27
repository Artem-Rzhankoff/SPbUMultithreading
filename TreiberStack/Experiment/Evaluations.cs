using System.Diagnostics;
using TreiberStack;
using static System.Threading.Interlocked;

namespace Experiment;

public class Evaluations
{
   private static Stopwatch _stopwatch = new();

   private static void _evaluate100(MyConcurrentStack<string> stack)
   {
      for (var i = 0; i < 15; i++)
      {
         stack.Push(i.ToString());
      }

      for (var i = 0; i < 10; i++)
      {
         stack.Pop();
      }
      
      stack.Push("push me please");

      for (var i = 0; i < 49; i++)
      {
         stack.Push(i.ToString());
      }
   }

   private static void _evaluatePushesAndPops100(MyConcurrentStack<string> stack)
   {
      for (var i = 0; i < 1000; i++)
      {
         stack.Push(i.ToString());
         stack.Pop();
      }
   }

   private static void EvaluateStackOperations(MyConcurrentStack<string> stack)
   {
      var threadsCount = Environment.ProcessorCount;

      var threads = new Thread[threadsCount];

      for (var i = 0; i < threadsCount; i++)
      {
         threads[i] = new Thread(() => _evaluate100(stack)); // тут мб делегат потом заюзать??
      }
      
      foreach (var thread in threads)
      {
         thread.Start();
      }

      foreach (var thread in threads)
      {
         thread.Join();
      }
   }

   public static void TimeSpend()
   {
      
      _stopwatch.Reset();
      
      var concurrentStack = new MyConcurrentStack<string>();
      var eliminationBackoffStack = new EliminationBackoffStack<string>();

      var results = new double[2];
      for (var i = 0; i < 2; ++i)
      {
         Console.WriteLine("asfdasdfafa");
         _stopwatch.Start();
         EvaluateStackOperations(i == 1 ? concurrentStack : eliminationBackoffStack);
         _stopwatch.Stop();
         results[i] = _stopwatch.ElapsedMilliseconds;
         _stopwatch.Reset();
      }
      
      Console.WriteLine($"{concurrentStack.Pop() == eliminationBackoffStack.Pop()}");
      Console.WriteLine($"concurrent: {results[0]}, elimination: {results[1]}");
   }
}
