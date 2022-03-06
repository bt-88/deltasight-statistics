using System.Collections.Immutable;

namespace DeltaSight.Statistics;

public record AdvancedStatistics : SimpleStatistics
{
    public double Minimum { get; init; }
    public double Maximum { get; init; }
    public double GreatestCommonDivisor { get; init; }
    
    /// <summary>
    /// Sorted probability densities
    /// </summary>
    public ImmutableDictionary<double, double> Probabilities { get; set; } = ImmutableDictionary<double, double>.Empty;

    public virtual bool Equals(AdvancedStatistics? other)
    {
        if (other is null) return false;

        return Minimum.Equals(other.Minimum)
               && Maximum.Equals(other.Maximum)
               && GreatestCommonDivisor.Equals(other.GreatestCommonDivisor)
               && Probabilities.ContentEquals(other.Probabilities);
    }
}