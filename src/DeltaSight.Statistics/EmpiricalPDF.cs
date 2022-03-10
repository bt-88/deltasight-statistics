using System.Collections.Immutable;
using DeltaSight.Statistics.Abstractions;

namespace DeltaSight.Statistics;

public class EmpiricalPDF<T> : IEmpiricalPDF<T> where T : struct, IComparable
{
    #region Static Creators
    
    /// <summary>
    /// Creates a new empirical probability density function from a sorted set of densities
    /// </summary>
    /// <param name="sortedProbabilityDensities">Sorted probability densities</param>
    /// <param name="checkSortOrder">If true, the sort order of the densities will be validated</param>
    /// <returns>A new empirical pdf</returns>
    public static EmpiricalPDF<T> FromSorted(IReadOnlyDictionary<T, double> sortedProbabilityDensities, bool checkSortOrder = false)
    {
        if (checkSortOrder) sortedProbabilityDensities.EnsureAscendingSortOrder();
        
        return new EmpiricalPDF<T>(sortedProbabilityDensities);
    }
    
    

    /// <summary>
    /// Creates a new empirical probability density function from an unsorted set of densities
    /// </summary>
    /// <param name="unsortedProbabilityDensities"></param>
    /// <returns></returns>
    public static EmpiricalPDF<T> FromUnsorted(IReadOnlyDictionary<T, double> unsortedProbabilityDensities)
    {
        switch (unsortedProbabilityDensities)
        {
            case SortedDictionary<T, double> sd:
                return new EmpiricalPDF<T>(sd);
            case ImmutableSortedDictionary<T, double> isd:
                return new EmpiricalPDF<T>(isd);
            default:
            {
                var sorted = unsortedProbabilityDensities
                    .OrderBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Value);
        
                return new EmpiricalPDF<T>(sorted);
            }
        }
    }

    #endregion
    
    #region Constructors

    private EmpiricalPDF(IReadOnlyDictionary<T, double> sortedProbabilities)
    {
        Probabilities = sortedProbabilities;
    }
    
    #endregion

    public IEmpiricalCDF<T> ToCDF() => EmpiricalCDF<T>.FromSorted(Probabilities);

    public double PrEquals(T x)
    {
        return Probabilities.TryGetValue(x, out var pr) ? pr : 0d;
    }
    
    /// <summary>
    /// Probability densities
    /// <remarks>Sorted on <typeparam name="T"></typeparam></remarks>
    /// </summary>
    public IReadOnlyDictionary<T, double> Probabilities { get; }
    
}