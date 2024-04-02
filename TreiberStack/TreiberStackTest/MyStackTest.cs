using System.Diagnostics;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using TreiberStack;

namespace TreiberStackTest;

[TestFixture]
public class MyStackTest
{

    private readonly Configuration _testConfiguration = Configuration.Create().WithTestingIterations(10).WithDeadlockTimeout(5000);
    
    private static void PrepareEnvironmentAndRunTest(MyConcurrentStack<string> stack, Action<MyConcurrentStack<string>> myDelegate)
    {
        var threadCount = Environment.ProcessorCount;
        
        Console.WriteLine(threadCount);

        var threads = new Thread[threadCount];

        for (var i = 0; i < threadCount; ++i)
        {
            threads[i] = new Thread(() => myDelegate(stack));
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

    private MyConcurrentStack<string> _stack;
    
    [SetUp]
    public void Setup()
    {
        _stack = new EliminationBackoffStack<string>();
    }
    
    [NUnit.Framework.Test]
    // доказательство корректности работы инструмента
    public void TestCodeBlockWithPotentialLivelock()
    {
        var testWithPotentiallyLivelock = () =>
        {
            var threads = new Thread[2];
            bool[] flags = [false, false];

            for (var i = 0; i < 2; ++i)
            {
                var i1 = i; // для избежания "Modified captured variable"
                threads[i] = new Thread(() =>
                {
                    while (!flags[1 - i1])
                    {
                        Thread.Sleep(100);
                        flags[1 - i1] = false;
                    }
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
        };
        
        var engine = TestingEngine.Create(_testConfiguration, () => testWithPotentiallyLivelock());
        
        engine.Run();
        Assert.IsFalse(engine.TestReport.NumOfFoundBugs == 0);
        engine.Stop();
    }
    
    [NUnit.Framework.Test]
    public void TestPushAndPopOperationsWithEqualAmount()
    {
        var testMethod = (MyConcurrentStack<string> stack) =>
        {
            for (var i = 0; i < 100; ++i)
            {
                stack.Push("p");
                stack.Pop();
            }
        };
        
        var engine = TestingEngine.Create(_testConfiguration, () => PrepareEnvironmentAndRunTest(_stack, testMethod));
        
        engine.Run();
        Assert.That(engine.TestReport.NumOfFoundBugs == 0);
        engine.Stop();

        Assert.Throws<NullReferenceException>(() => _stack.TopElement());
    }

    [NUnit.Framework.Test]
    public void TestInvariantOfStackSizeAfterOperations()
    {
        var testMethod = (MyConcurrentStack<string> stack) =>
        {
            for (var i = 0; i < 150; ++i) 
            {
                stack.Push("p");
            }
            for (var i = 0; i < 100; ++i)
            {
                stack.Pop();
            }

            for (var i = 0; i < 50; ++i)
            {
                stack.Push("p");
            }

        };
        
        var engine = TestingEngine.Create(_testConfiguration, () => PrepareEnvironmentAndRunTest(_stack, testMethod));
        
        engine.Run();
        Assert.That(engine.TestReport.NumOfFoundBugs == 0);
        engine.Stop();

        var stackElementsCount = 0;
        while (true)
        {
            try
            {
                _stack.Pop();
                ++stackElementsCount;
            }
            catch (NullReferenceException)
            {
                break;
            }
            
        }
        Assert.AreEqual(100 * 10 * Environment.ProcessorCount, stackElementsCount);
    }
}