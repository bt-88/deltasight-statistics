namespace DeltaSight.Statistics.Abstractions;

/// <summary>
/// Empirical probability density function
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IEmpiricalPDF<T> where T : struct, IComparable
{
    /// <summary>
    /// All entries for the density function of X <c>Pr{X=x}</c>
    /// <remarks>Sorted ascending on <typeparam name="T">Value</typeparam></remarks>
    /// </summary>
    IReadOnlyDictionary<T, double> Probabilities { get; }

    IEmpiricalCDF<T> ToCDF();

    /// <summary>
    /// Returns the probability that X equals <paramref name="x"/> based on the empirical probability densities
    /// </summary>
    double PrEquals(T x);
}