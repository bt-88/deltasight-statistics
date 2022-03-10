using DeltaSight.Statistics.Abstractions;

namespace DeltaSight.Statistics;

public record SimpleStatistics : IStatisticsSnapshot
{
    public static SimpleStatistics Empty = new();
    
    /// <summary>Value count</summary>
    public long Count { get; init; }
    
    /// <summary>
    /// Number of values equal to zero
    /// </summary>
    public long CountZero { get; init; }

    /// <summary>
    /// Mean or average value
    ///<remarks>Is NaN if the count is zero</remarks>
    /// </summary>
    public double Mean { get; init; } = double.NaN;

    /// <summary>Sum of all values</summary>
    public double Sum { get; init; }

    /// <summary>
    /// Sample variance based on n-1 degrees of freedom
    /// <remarks>Is zero if n=1</remarks>
    /// </summary>
    public double Variance { get; init; }

    /// <summary>Population variance based on n degrees of freedom</summary>
    public double PopulationVariance { get; init; }

    /// <summary>
    /// Sample standard deviation based on n-1 degrees of freedom
    /// <remarks>Is zero if <see cref="Variance"/> is zero</remarks>
    /// </summary>
    public double StandardDeviation { get; init; }

    /// <summary>
    /// Population standard deviation based on n degrees of freedom
    /// <remarks>Is zero if <see cref="PopulationVariance"/> is zero</remarks>
    /// </summary>
    public double PopulationStandardDeviation { get; init; }

    /// <summary>
    /// Sum of the squared errors
    /// </summary>
    public double SumSquaredError { get; init; }
    
    /// <summary>
    /// <see cref="PopulationStandardDeviation"/> divided by the <see cref="Mean"/>
    /// <remarks>Is zero if the <see cref="Mean"/> is zero</remarks>
    /// </summary>
    public double PopulationCoefficientOfVariation { get; init; }
    
    /// <summary>
    /// <see cref="StandardDeviation"/> divided by the <see cref="Mean"/>
    /// <remarks>Is zero if the <see cref="Mean"/> is zero</remarks>
    /// </summary>
    public double CoefficientOfVariation { get; init; }
}