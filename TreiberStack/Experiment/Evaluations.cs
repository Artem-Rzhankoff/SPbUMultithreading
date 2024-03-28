using System.Diagnostics;
using TreiberStack;

namespace Experiment;

public class Evaluations
{
   private static readonly Stopwatch _stopwatch = new();

   private static double _evaluateStackOperationsRandom(MyConcurrentStack<string> stack)
   {
      var randomBoolsArray = new bool[500_000];
      var random = new Random();

      for (var i = 0; i < 500_000; ++i)
      {
         randomBoolsArray[i] = random.Next(2) == 0;
         stack.Push("p");
      }
      
      _stopwatch.Start();
      for (var i = 0; i < 500_000; ++i)
      {
         if (randomBoolsArray[i])
         {
            stack.Push("p");
         }
         else
         {
            stack.Pop();
         }
      }

      var spendTimeNano = _stopwatch.ElapsedMilliseconds * 1_000_000;
      var operationsPerSecond = 500_000 * 10e9 / spendTimeNano;
      _stopwatch.Reset();

      return operationsPerSecond;
   }
   
   private static double _evaluateStackOperations(MyConcurrentStack<string> stack)
   {
      _stopwatch.Start();
      for (var i = 0; i < 500_000; ++i)
      {
         stack.Push("p");
         stack.Pop();
      }

      var spendTimeNano = _stopwatch.ElapsedMilliseconds * 1_000_000;
      var operationsPerSecond = 500_000 * 10e9 / spendTimeNano;
      _stopwatch.Reset();

      return operationsPerSecond;
   }

   private static double EvaluateStackOperations(MyConcurrentStack<string> stack, int numberOfThreads, Func<MyConcurrentStack<string>, double> myMethod)
   {
      var threads = new Thread[numberOfThreads];

      double maxSpendingTime = 0; // за время работы будет считаться время, потраченное самым непроизводительным потоком

      for (var i = 0; i < numberOfThreads; i++)
      {
         threads[i] = new Thread(() =>
         {
            var time = myMethod(stack);
            maxSpendingTime = Math.Max(maxSpendingTime, time);
         });
      }
      
      foreach (var thread in threads)
      {
         thread.Start();
      }

      foreach (var thread in threads)
      {
         thread.Join();
      }

      return maxSpendingTime;
   }

   public static void TimeSpend()
   {

      for (var j = 0; j < 2; ++j)
      {
         var typeOfStack = j == 0
            ? ExperimentData.TypeOfStack.TreiberStack
            : ExperimentData.TypeOfStack.EliminationBackoffStack;

         var averages = new double[Environment.ProcessorCount];
         var randomAverages = new double[Environment.ProcessorCount];

         for (var threadsAmount = 1; threadsAmount <= Environment.ProcessorCount; ++threadsAmount)
         {
            double average = 0;
            double randomAverage = 0;

            for (var i = 0; i < 20; ++i)
            {
               var (stack, randomPaddingStack) = j == 0
                  ? (new MyConcurrentStack<string>(), new MyConcurrentStack<string>())
                  : (new EliminationBackoffStack<string>(), new EliminationBackoffStack<string>());
               
               average = (average * i + EvaluateStackOperations(stack, threadsAmount, _evaluateStackOperations)) / (i + 1);
               randomAverage =
                  (randomAverage * i + EvaluateStackOperations(randomPaddingStack, threadsAmount, _evaluateStackOperationsRandom)) /
                  (i + 1);
            }

            averages[threadsAmount] = average;
            randomAverages[threadsAmount] = randomAverage;
         }

         var first = new ExperimentData(averages, false, typeOfStack);
         var second = new ExperimentData(randomAverages, true, typeOfStack);

         EvaluationsWriter.WriteTable(first, second);
      }
   }
   
   
}
