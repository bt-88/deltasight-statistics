using System.Collections.Immutable;

namespace DeltaSight.Statistics;

public record AdvancedStatistics : SimpleStatistics
{
    public double Minimum { get; init; }
    public double Maximum { get; init; }
    public double GreatestCommonDivisor { get; init; }
    public IReadOnlyDictionary<double, double> Probabilities { get; set; } = ImmutableDictionary<double, double>.Empty;
    
}