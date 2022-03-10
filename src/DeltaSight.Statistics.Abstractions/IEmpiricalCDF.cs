namespace DeltaSight.Statistics.Abstractions;

/// <summary>
/// Empirical cumulative probability density function
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IEmpiricalCDF<T> where T : struct, IComparable
{
    /// <summary>
    /// The probability that X is smaller than or equal to <paramref name="x"/> based on the empirical densities
    /// </summary>
    double PrLessThanOrEqual(T x);
    T Maximum { get; }
    T Minimum { get; }
}