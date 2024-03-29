using System.Diagnostics;
using TreiberStack;

namespace MyExperiment;

public class Evaluations
{
   private static readonly Stopwatch _stopwatch = new();

   private static double _evaluateStackOperationsRandom(MyConcurrentStack<string> stack)
   {
      var randomBoolsArray = new bool[100_000];
      var random = new Random();

      for (var i = 0; i < 100_000; ++i)
      {
         randomBoolsArray[i] = random.Next(2) == 0;
         stack.Push("p");
      }

      var stopwatch = new Stopwatch();
      
      stopwatch.Start();
      for (var i = 0; i < 100_000; ++i)
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

      var spendTimeNano = stopwatch.ElapsedMilliseconds * 1_000_000;
      var operationsPerSecond = 100_000 * 10e9 / (spendTimeNano == 0 ? 10e-9 : spendTimeNano);
      stopwatch.Reset();

      return operationsPerSecond;
   }
   
   private static double _evaluateStackOperations(MyConcurrentStack<string> stack)
   {
      for (var i = 0; i < 100_000; ++i)
      {
         stack.Push("p");
      }

      var stopwatch = new Stopwatch();
      
      stopwatch.Start();
      for (var i = 0; i < 100_000; ++i)
      {
         stack.Push("p");
         stack.Pop();
      }
      stopwatch.Stop();
      
      var spendTimeNano = stopwatch.ElapsedMilliseconds * 1_000_000;
      var operationsPerSecond = 100_000 * 10e9 / (spendTimeNano == 0 ? 10e-9 : spendTimeNano);

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
            var time = myMethod(stack) / 1_000_000;
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

      for (var j = 0; j < 2; ++j) // рандом или нет
      {
         var isRandom = j == 1;
         var operationType  = (MyConcurrentStack<string> stack) =>  
            isRandom ? _evaluateStackOperationsRandom(stack) : _evaluateStackOperations(stack);

         var averages = new double[Environment.ProcessorCount];
         var eliminationAverages = new double[Environment.ProcessorCount];

         for (var threadsAmount = 1; threadsAmount <= Environment.ProcessorCount; ++threadsAmount)
         {
            double averageTreiber = 0;
            double averageElimination = 0;

            for (var i = 0; i < 20; ++i)
            {
               var (treiberStack, eliminationStack) = (new MyConcurrentStack<string>(), new EliminationBackoffStack<string>());
               
               averageTreiber = (averageTreiber * i + EvaluateStackOperations(treiberStack, threadsAmount, operationType)) / (i + 1);
               averageElimination =
                  (averageElimination * i + EvaluateStackOperations(eliminationStack, threadsAmount, operationType)) /
                  (i + 1);
            }

            averages[threadsAmount-1] = averageTreiber;
            eliminationAverages[threadsAmount-1] = averageElimination;
         }

         var first = new ExperimentData(averages, isRandom, ExperimentData.TypeOfStack.TreiberStack);
         var second = new ExperimentData(eliminationAverages, isRandom, ExperimentData.TypeOfStack.EliminationBackoffStack);

         EvaluationsWriter.WriteTable(first, second);
      }
   }
   
   
}
