namespace MyExperiment;

public record ExperimentData
{
    public enum TypeOfStack
    {
        TreiberStack,
        EliminationBackoffStack
    }

    public (double OperationsPerSecond, int NumberOfThreads)[] Data { get; init; }
    public bool IsRandomPadding { get; init; }
    public TypeOfStack StackImplementation { get; init; }

    public ExperimentData(double[] operationsTime, bool isRandom, TypeOfStack type)
    {
        Data = operationsTime.Select((x, index) => (x, index)).ToArray();
        IsRandomPadding = isRandom;
        StackImplementation = type;
    }
}