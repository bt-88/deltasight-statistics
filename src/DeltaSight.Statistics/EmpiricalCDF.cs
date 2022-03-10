using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DeltaSight.Statistics.Abstractions;

namespace DeltaSight.Statistics;

public static class SortExtensions
{
    /// <summary>
    /// Throws if <paramref name="entries"/> is not sorted ascending on key <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Key</typeparam>
    public static void EnsureAscendingSortOrder<T>(
        this IEnumerable<KeyValuePair<T, double>> entries,
        [CallerArgumentExpression("entries")] string entriesName = "") where T : struct, IComparable
    {
        var last = default(T);
        
        foreach (var (key, _) in entries)
        {
            key.EnsureItSucceeds(last, entriesName);

            last = key;
        }
    }

    /// <summary>
    /// Throws if <paramref name="b"/> precedes <paramref name="a"/>
    /// </summary>
    public static void EnsureItSucceeds<T>(this T a, T b, string collectionName = "") where T : IComparable
    {
        if (a.CompareTo(b) < 0) throw new ArgumentException($"Keys are not sorted ascending: '{b}' > '{a}'", collectionName);
    }
}

public class EmpiricalCDF<T> : IEmpiricalCDF<T>
    where T : struct, IComparable
{
    private readonly IReadOnlyDictionary<T, double> _cumulativeProbabilities;
    private readonly IReadOnlyList<T> _sortedKeys;

    private const double Tolerance = 1e-10;

    #region Constructors

    private EmpiricalCDF(IReadOnlyDictionary<T, double> cumulativeDensities, IReadOnlyList<T> keys)
    {
        _cumulativeProbabilities = cumulativeDensities;
        _sortedKeys = keys;
    }
    
    #endregion

    #region Static creators

    private static EmpiricalCDF<T> Create(IReadOnlyDictionary<T, double> prDensities, bool checkSortOrder)
    {
        var cumulativeDensities = new Dictionary<T, double>();
        var keys = new List<T>();
        var prCumulative = 0d;
        var prSum = 0d;
        var last = default(T);

        foreach (var (key, value) in prDensities)
        {
            if (checkSortOrder) key.EnsureItSucceeds(last);
            
            prSum += value;
            prCumulative += value;
            
            cumulativeDensities.Add(key, prCumulative);
            keys.Add(key);

            last = key;
        }

        var delta = 1d - prSum;

        if (Math.Abs(delta) > Tolerance)
            throw new InvalidOperationException(
                $"The densities do not sum to 1d: Abs. delta ({delta:e2}) is greater than tolerance ({Tolerance:e2})");

        cumulativeDensities[keys[^1]] += delta; // Add delta to last probability

        return new EmpiricalCDF<T>(cumulativeDensities, keys);
    }
    
    /// <summary>
    /// Creates a cumulative density function (cdf) from unsorted empirical probabilities
    /// </summary>
    /// <param name="unsortedDensities">Empirical probability density function</param>
    /// <returns>A new empirical cdf</returns>
    /// <exception cref="InvalidOperationException">Probabilities do not sum to 1d</exception>
    public static EmpiricalCDF<T> FromUnsorted(IReadOnlyDictionary<T, double> unsortedDensities)
    {
        switch (unsortedDensities)
        {
            case SortedDictionary<T, double> sd:
                return Create(unsortedDensities, false);
            case ImmutableSortedDictionary<T, double> isd:
                return Create(unsortedDensities, false);
            default:
            {
                var sorted = unsortedDensities
                    .OrderBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Value);

                return Create(sorted, false);
            }
        }
        
        return Create(unsortedDensities.ToImmutableSortedDictionary(
            x => x.Key, 
            x => x.Value), false);
    }
    
    /// <summary>
    /// Creates a cumulative density function (cdf) from sorted empirical probabilities
    /// </summary>
    /// <param name="prDensities">Empirical probability density function sorted on key</param>
    /// <returns>A new empirical cdf</returns>
    /// <exception cref="InvalidOperationException">Probabilities do not sum to 1d</exception>
    public static EmpiricalCDF<T> FromSorted(IReadOnlyDictionary<T, double> prDensities)
    {
        return Create(prDensities, true);
    }
    
    #endregion

    #region Private members
    
    private int IndexOfBelow(T x)
    {            
        var index = BinarySearchIndexOf(_sortedKeys,x);

        return index < 0 ? ~index - 1 : index;
    }
    
    /// <summary>
    /// Looks up the index of <paramref name="value"/> from <paramref name="list"/>.
    /// </summary>
    /// <typeparam name="T">Type of sorted list.</typeparam>
    /// <param name="list">List of sorted values.</param>
    /// <param name="value">Value of <typeparamref name="T"/> to look up. Value can be null for reference types.</param>
    /// <returns>The zero-based index of item in the sorted <paramref name="list"/>, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of Count.</returns>
    private static int BinarySearchIndexOf(IReadOnlyList<T> list, T value)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        var lower = 0;
        var upper = list.Count - 1;

        while (lower <= upper)
        {
            var middle = lower + (upper - lower) / 2;
            var comparisonResult = value.CompareTo(list[middle]);
            if (comparisonResult == 0) return middle;
            
            if (comparisonResult < 0)
                upper = middle - 1;
            else
                lower = middle + 1;
        }

        return ~lower;
    }
    
    #endregion

    public T Minimum => _sortedKeys[0];
    public T Maximum => _sortedKeys[^1];
    
    public double PrLessThanOrEqual(T x)
    {
        if (x.CompareTo(Minimum) < 0) return 0d;
        if (x.CompareTo(Maximum) >= 0) return 1d;

        var index = IndexOfBelow(x);

        return _cumulativeProbabilities[_sortedKeys[index]];
    }
}