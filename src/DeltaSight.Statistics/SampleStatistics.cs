using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace DeltaSight.Statistics;

/// <summary>
/// Tracks the statistics for a sample of values
/// <remarks>
/// Allows for modification of the sample via <see cref="Add(double,long)"/> and <see cref="Remove(double, long)"/>.
/// The object is immutable and serializable.
/// </remarks>
/// </summary>
[Pure]
[Serializable]
public record SampleStatistics
{
    public static readonly SampleStatistics Empty = new();
    
    #region Constructors
    
    public SampleStatistics()
    {
    }
    
    public SampleStatistics(SampleStatistics other)
    {
        Sum = other.Sum;
        Count = other.Count;
        Variance = other.Variance;
        PopulationVariance = other.PopulationVariance;
        StandardDeviation = other.StandardDeviation;
        PopulationStandardDeviation = other.PopulationStandardDeviation;
    }
    
    #endregion

    #region Properties
    
    /// <summary>
    /// Number of values in the sample
    /// </summary>
    public long Count { get; init; }
    
    /// <summary>
    /// Average sample value
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? Mean { get; init; }
    
    /// <summary>
    /// Standard deviation of the sample (with n - 1 degrees of freedom)
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? StandardDeviation { get; init; }
    
    /// <summary>
    /// Standard deviation of the entire population (n degrees of freedom)
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? PopulationStandardDeviation { get; init; }
    
    /// <summary>
    /// Variance of the sample (with n - 1 of degrees of freedom)
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? Variance { get; init; }
    
    /// <summary>
    /// Variance of the population (with n degrees of freedom)
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? PopulationVariance { get; init; }
    
    /// <summary>
    /// Sum of the values in the sample
    /// </summary>
    public double Sum { get; init; }
    
    #endregion

    #region Public functions
    
    [MemberNotNullWhen(false, nameof(Mean), nameof(StandardDeviation), nameof(PopulationStandardDeviation), nameof(Variance), nameof(PopulationVariance))]
    public bool IsEmpty() => Count == 0L;
    
    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the addition of <paramref name="value"/>
    /// </summary>
    /// <param name="value">Value to add to the sample</param>
    /// <param name="count">Number of times to add the value</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">If count is lower than zero</exception>
    [Pure]
    public SampleStatistics Add(double value, long count = 1L)
    {
        if (count < 1L) throw new ArgumentOutOfRangeException(nameof(count));

        if (IsEmpty()) return FromSingleValue(value, count);
        
        var n = Count + count;
        var d = value - Mean.Value;
        var s = d / n;
        var t = d * s * (n - 1L);

        var m2 = PopulationVariance.Value * Count + t;
        var mean = Mean.Value + d / n * count;
        
        return FromM2(n, mean, m2);
    }

    /// <summary>
    /// Create sample statistics for a sample with a single value in it
    /// </summary>
    /// <param name="value">Value</param>
    /// <param name="count">Number of times the value is in the sample</param>
    [Pure]
    public static SampleStatistics FromSingleValue(double value, long count = 1)
    {
        return new SampleStatistics
        {
            Mean = value,
            StandardDeviation = 0d,
            PopulationStandardDeviation = 0d,
            Variance = 0d,
            PopulationVariance = 0d,
            Count = count,
            Sum = value * count
        };
    }
    
    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the removal of <paramref name="value"/>
    /// </summary>
    /// <param name="value">Value to remove from the sample</param>
    /// <param name="count">Number of times to remove the value</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">If count is lower than zero</exception>
    /// <exception cref="InvalidOperationException">If the sample is empty or if the count to be removed is greater than the sample count</exception>
    [Pure]
    public SampleStatistics Remove(double value, long count = 1L)
    {
        if (count < 1L) throw new ArgumentOutOfRangeException(nameof(count));
        
        var n = Count - count;

        if (IsEmpty()) throw new InvalidOperationException("Running statistics is empty: nothing to remove");
        if (n < 0L) throw new InvalidOperationException($"{nameof(count)} is greater than added count");
        
        if (n == 1L) return FromSingleValue(value, count);
        if (n == 0L) return Empty;
        
        var newMean = (Sum - value * count) / n;

        var t1 = (Count - 1) * Variance.Value - (value - Mean.Value) * (value - newMean) * count;

        //double newVariance;
        double newPopVariance;

        if (t1 < 0)
        {
            //newVariance = 0d;
            newPopVariance = 0d;
        }
        else
        {
            //newVariance = t1 / (n - 1);
            newPopVariance = t1 / n;    
        }

        return FromM2(n, newMean, newPopVariance * n);
    }

    /// <summary>
    /// Combines two sample by adding them up
    /// </summary>
    /// <param name="other">Sample statistics to add</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Add(SampleStatistics other)
    {
        if (IsEmpty() && other.IsEmpty()) return Empty;
        if (IsEmpty()) return new SampleStatistics(other);
        if (other.IsEmpty()) return new SampleStatistics(this);

        var n = Count + other.Count;
        var d = other.Mean.Value - Mean.Value;
        var d2 = d * d;

        var mean = (Count * Mean.Value + other.Count * other.Mean.Value) / n;
        var m2 = M2() + other.M2() + d2 * Count * other.Count / n;

        return FromM2(n, mean, m2);
    }
    
    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the removal of multiple <paramref name="values"/>
    /// </summary>
    /// <param name="values">Values to remove from the sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Remove(IEnumerable<double> values) => RemoveMultiple(values);

    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the removal of multiple <paramref name="values"/>
    /// </summary>
    /// <param name="values">Values to remove from the sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Remove(params double[] values) => RemoveMultiple(values);

    
    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the addition of multiple <paramref name="values"/>
    /// </summary>
    /// <param name="values">Values to add to the sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Add(IEnumerable<double> values) => AddMultiple(values);

    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the addition of multiple <paramref name="values"/>
    /// </summary>
    /// <param name="values">Values to add to the sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Add(params double[] values) => AddMultiple(values);

    /// <summary>
    /// Multiplies every value in the sample by a <paramref name="multiplier"/> and returns a new <see cref="SampleStatistics"/>
    /// </summary>
    /// <param name="multiplier">Multiplier</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Multiply(double multiplier)
    {
        if (IsEmpty()) return Empty;
        
        return FromM2(Count, Mean.Value * multiplier, M2() * multiplier * multiplier);
    }
    
    #endregion
    
    #region Private functions
    
    private double M2() => (PopulationVariance ?? 0d) * Count;
    
    private static SampleStatistics FromM2(long n, double mean, double m2)
    {
        var variance = m2 / (n - 1L);
        var popVariance = m2 / n;
        
        return new SampleStatistics
        {
            Sum = n * mean,
            Mean = mean,
            Count = n,
            Variance = variance,
            PopulationVariance = popVariance,
            StandardDeviation = Math.Sqrt(variance),
            PopulationStandardDeviation = Math.Sqrt(popVariance),
        };
    }
    
    private SampleStatistics AddMultiple(IEnumerable<double> values)
    {
        return values.Aggregate(this, (current, value) => current.Add(value));
    }

    private SampleStatistics RemoveMultiple(IEnumerable<double> values)
    {
        return values.Aggregate(this, (current, value) => current.Remove(value));
    }
    
    #endregion

    #region Operators
    
    public static SampleStatistics operator +(SampleStatistics a, SampleStatistics b)
    {
        return a.Add(b);
    }

    public static SampleStatistics operator *(SampleStatistics a, double multiplier)
    {
        return a.Multiply(multiplier);
    }

    #endregion
}