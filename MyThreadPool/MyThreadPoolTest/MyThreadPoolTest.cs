using MyThreadPoolLibrary;

namespace MyThreadPoolTest;

public class MyThreadPoolTest
{
    private MyThreadPool _myThreadPool;
    private readonly int _threadsNumber = 6;
    
    [SetUp]
    public void Setup()
    {
        _myThreadPool = new MyThreadPool(_threadsNumber);
    }

    [TearDown]
    public void ShutdownThreadPool()
    {
        _myThreadPool.Dispose();
    }

    [Test, Repeat(10)]
    public void EnqueueTask_ReturnCorrectResult()
    {
        const int expectedResult = 52;
        var task = _myThreadPool.Enqueue(() => 52);
        Assert.That(task.Result, Is.EqualTo(expectedResult));
    }

    [Test, Repeat(10)]
    public void MultipleEnqueueTask_ReturnCorrectResult()
    {
        const int expectedResult = 10;
        var count = 0;
        
        for (var i = 0; i < expectedResult; ++i)
        {
            _ = _myThreadPool.Enqueue(() => count++).Result;
        }
        
        Assert.That(count, Is.EqualTo(expectedResult));
    }

    [Test, Repeat(10)]
    public void ContinuationOfStillRunningTask_ReturnCorrectResult()
    {
        const int expectedResult = 52;

        var task = _myThreadPool.Enqueue(() =>
        {
            Thread.Sleep(1000);
            return 51;
        });
        var continuationTask = task.ContinueWith(almost => ++almost);
        
        Assert.That(continuationTask.Result, Is.EqualTo(expectedResult));
    }

    [Test, Repeat(10)]
    public void NestedContinuationOfStillRunningTask_ReturnCorrectResult()
    {
        var expectedResult = _threadsNumber;

        var task = _myThreadPool.Enqueue(() =>
        {
            Thread.Sleep(1000);
            return 0;
        });

        for (var i = 0; i < _threadsNumber; ++i)
        {
            task = task.ContinueWith(counter => ++counter);
        }
        
        Assert.That(task.Result, Is.EqualTo(expectedResult));
    }

    [Test, Repeat(10)]
    public void MultipleGetResultOfTask_ReturnCorrectResult()
    {
        var expectedResult = 1;

        var task = _myThreadPool.Enqueue(() => expectedResult);

        for (var i = 0; i < 10; ++i)
        {
            Assert.That(task.Result, Is.EqualTo(expectedResult));
        }
    }

    [Test, Repeat(10)]
    public void DisposeThreadPoolWithRunningTasks_CurrentlyRunningTasksShouldBeComplete_WithReturnCorrectResult()
    {
        var expectedResult = 5;
        
        var task = _myThreadPool.Enqueue(() =>
        {
            Thread.Sleep(1000);
            return expectedResult;
        });
        
        Thread.Sleep(500);
        _myThreadPool.Dispose();
        
        Assert.That(task.Result, Is.EqualTo(expectedResult));
    }

    [Test, Repeat(10)]
    public void RunManyTasksOnThreadPool_ReturnCorrectResult()
    {
        var expectedResult = _threadsNumber * 2;
        var counter = 0;

        for (var i = 0; i < _threadsNumber * 2; ++i)
        {
            _myThreadPool.Enqueue(() => ++counter);
        }
        
        Thread.Sleep(2000);
        
        Assert.That(counter, Is.EqualTo(expectedResult));
    }

    [Test, Repeat(10)]
    public void RunTaskForEachThreadParallel_ReturnCorrectResult_WithFullLoadThreads()
    {
        var expectedResult = _threadsNumber;
        var counter = 0;

        for (var i = 0; i < _threadsNumber; ++i)
        {
            _myThreadPool.Enqueue(() =>
            {
                Interlocked.Increment(ref counter);
                while (counter < _threadsNumber)
                {
                }
            });
        }
        
        Thread.Sleep(1000);
        
        Assert.That(counter, Is.EqualTo(expectedResult));
    }

    [Test, Repeat(10)]
    public void ContinuationOfTaskThatFinishedWithException_ReturnCorrectResult()
    {
        var expectedResult = 0;
        var notTrueEqualsVariable = false;
        var task = _myThreadPool.Enqueue(() =>
        {
            Thread.Sleep(100);
            return notTrueEqualsVariable ? 5 : throw new Exception();
        });

        var continuationTask = task.ContinueWith(result =>
        {
            try
            {
                return result.Result;
            }
            catch (AggregateException)
            {
                return expectedResult;
            }
        });
        
        Thread.Sleep(1000);
        
        Assert.That(continuationTask.Result, Is.EqualTo(expectedResult));
        
    }
    
    [Test, Repeat(10)]
    public void TaskWithMethodThatThrowException_ThrowAggregateException()
    {
        var exception = new Exception();
        var variableNotEqualToTrue = false;
        var task = _myThreadPool.Enqueue(() => variableNotEqualToTrue ? 1 : throw exception);

        var aggregateException = Assert.Throws<AggregateException>(() => _ = task.Result);
        CollectionAssert.Contains(aggregateException.InnerExceptions, exception);
    }

    [Test, Repeat(10)]
    public void DisposeThreadPoolWithTasksInWaitingQueue_NotRunningTasksShouldCanceled_WithThrowException()
    {
        _myThreadPool = new MyThreadPool(1);
        _myThreadPool.Enqueue(() =>
        {
            Thread.Sleep(2000);
            return 52;
        });
        var task = _myThreadPool.Enqueue(() => 52);
        
        Thread.Sleep(500);
        _myThreadPool.Dispose();
        
        Assert.Throws<OperationCanceledException>(() => _ = task.Result);
    }
    
    [Test, Repeat(10)]
    public void AddContinuationTaskAfterDisposeThreadPool_ThrowException()
    {
        var task = _myThreadPool.Enqueue(() =>
        {
            Thread.Sleep(1000);
            return 5;
        });
        Thread.Sleep(500);
        _myThreadPool.Dispose();
        
        Assert.Throws<OperationCanceledException>(() => task.ContinueWith(result => result + 1));
    }
}