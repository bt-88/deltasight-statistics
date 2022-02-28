namespace DeltaSight.Statistics;

public record SimpleStatistics
{

    //public static readonly Statistics Empty = new();

    /// <summary>Value count</summary>
    public long Count { get; init; }

    /// <summary>Mean or average value</summary>
    public double Mean { get; init; }

    /// <summary>Sum of all values</summary>
    public double Sum { get; init; }

    /// <summary>Sample variance based on n-1 degrees of freedom</summary>
    public double Variance { get; init; }

    /// <summary>Population variance based on n degrees of freedom</summary>
    public double PopulationVariance { get; init; }

    /// <summary>Sample standard deviation based on n-1 degrees of freedom</summary>
    public double StandardDeviation { get; init; }

    /// <summary>Population standard deviation based on n-1 degrees of freedom</summary>
    public double PopulationStandardDeviation { get; init; }
    
    /// <summary>
    /// Sum of the squared errors
    /// </summary>
    public double SumSquaredError { get; init; }
}