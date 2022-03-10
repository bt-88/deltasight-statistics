using System.Collections.Immutable;

namespace DeltaSight.Statistics;

public record AdvancedStatistics : SimpleStatistics
{
    public new static readonly AdvancedStatistics Empty = new();
    
    /// <summary>
    /// Minimum value
    /// <remarks>Is NaN if count is zero</remarks>
    /// </summary>
    public double Minimum { get; init; } = double.NaN;
    
    /// <summary>
    /// Maximum value
    /// <remarks>Is NaN if count is zero</remarks>
    /// </summary>
    public double Maximum { get; init; } = double.NaN;
    
    /// <summary>
    /// Greatest common divisor
    /// <remarks>Is NaN if count is zero</remarks>
    /// </summary>
    public double GreatestCommonDivisor { get; init; } = double.NaN;
    
    /// <summary>
    /// Sorted probability densities
    /// </summary>
    public ImmutableDictionary<double, double> Probabilities { get; init; } = ImmutableDictionary<double, double>.Empty;

    /// <summary>
    /// Creates an empirical probability density function from the <see cref="Probabilities"/>
    /// </summary>
    /// <returns>A new empirical probability density function (pdf)</returns>
    public EmpiricalPDF<double> CreatePDF() => EmpiricalPDF<double>.FromSorted(Probabilities);

    public virtual bool Equals(AdvancedStatistics? other)
    {
        if (other is null) return false;

        return Minimum.Equals(other.Minimum)
               && Maximum.Equals(other.Maximum)
               && GreatestCommonDivisor.Equals(other.GreatestCommonDivisor)
               && Probabilities.ContentEquals(other.Probabilities);
    }
}