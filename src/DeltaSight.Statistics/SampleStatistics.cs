using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// Creates statistics for a sample
    /// </summary>
    /// <param name="values">Value sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public static SampleStatistics From(IEnumerable<double> values) => Empty.AddOrRemoveMultiple(values, true);
    
    /// <summary>
    /// Creates statistics for a sample
    /// </summary>
    /// <param name="values">Value sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public static SampleStatistics From(params double[] values) => Empty.AddOrRemoveMultiple(values, true);

    /// <summary>
    /// Creates statistics for a sample with a single value in it
    /// </summary>
    /// <param name="value">Value</param>
    /// <param name="count">Number of times the value is in the sample</param>
    [Pure]
    public static SampleStatistics From(double value, long count = 1) => new (count, value * count, 0d);

    #region Constructors
    
    private SampleStatistics()
    {
        IsEmpty = true;
    }

    [JsonConstructor]
    public SampleStatistics(long count, double sum, double sumSquaredErrors)
    {
        Count = count;
        Sum = sum;
        SumSquaredErrors = sumSquaredErrors;
    }
    
    public SampleStatistics(SampleStatistics other)
    {
        Count = other.Count;
        Sum = other.Sum;
        SumSquaredErrors = other.SumSquaredErrors;
    }
    
    #endregion

    #region Properties

    /// <summary>
    /// Number of values in the sample
    /// </summary>
    public long Count { get; }

    /// <summary>
    /// Average sample value
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? Mean => IsEmpty ? null : Sum / Count;

    /// <summary>
    /// Standard deviation of the sample (with n - 1 degrees of freedom)
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? StandardDeviation
    {
        get
        {
            if (IsEmpty) return null;

            return CorrectVariance(Variance.Value);
        }
    }

    /// <summary>
    /// Standard deviation of the entire population (n degrees of freedom)
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? PopulationStandardDeviation
    {
        get
        {
            if (IsEmpty) return null;

            return CorrectVariance(PopulationVariance.Value);
        }
    }

    private double CorrectVariance(double value)
    {
        var stDev = Math.Sqrt(value);

        return double.IsNaN(stDev) ? 0d : stDev;
    }

    /// <summary>
    /// Variance of the sample (with n - 1 of degrees of freedom)
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? Variance => IsEmpty
        ? null
        : Count > 1L
            ? SumSquaredErrors / (Count - 1L)
            : 0d;

    /// <summary>
    /// Variance of the population (with n degrees of freedom)
    /// <remarks>Null if <see cref="Count"/> is zero</remarks>
    /// </summary>
    public double? PopulationVariance => IsEmpty ? null : SumSquaredErrors / Count;
    
    /// <summary>
    /// Sum of the values in the sample
    /// </summary>
    public double Sum { get; }
    
    /// <summary>
    /// Sum of Squared Errors (SSE)
    /// <remarks>
    /// The mean of this is the MSE and equals the <see cref="PopulationVariance"/>
    /// The mean root of this is the RMSE and equals the <see cref="PopulationStandardDeviation"/>
    /// </remarks>
    /// </summary>
    public double SumSquaredErrors { get; }
    
    #endregion

    #region Public functions
    
    [MemberNotNullWhen(false, nameof(Mean), nameof(StandardDeviation), nameof(PopulationStandardDeviation), nameof(Variance), nameof(PopulationVariance))]
    [JsonIgnore]
    public bool IsEmpty { get; private init; }

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

        if (IsEmpty) return From(value, count);
        
        var n = Count + count;
        var d = value - Mean.Value;
        var s = d / n;
        var t = d * s * (n - 1L);

        var stats = new SampleStatistics(n, Sum + value * count, SumSquaredErrors + t);

        return stats;
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
        if (IsEmpty) throw new InvalidOperationException("Running statistics is empty: nothing to remove");

        switch (Count - count)
        {
            case < 0L:
                throw new InvalidOperationException($"{nameof(count)} is greater than added count");
            case 1L:
                return From(value);
            case 0L:
                return Empty;
        }
        
        var (newCount, newSum, newSse) = Remove(value, count, Count, Sum, SumSquaredErrors);
        
        return new SampleStatistics(newCount, newSum, newSse);
    }

    /// <summary>
    /// Combines two sample by adding them up
    /// </summary>
    /// <param name="other">Sample statistics to add</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Add(SampleStatistics other)
    {
        if (IsEmpty && other.IsEmpty) return Empty;
        if (IsEmpty) return new SampleStatistics(other);
        if (other.IsEmpty) return new SampleStatistics(this);
        
        var n = Count + other.Count;
        var d = other.Mean.Value - Mean.Value;
        var d2 = d * d;
        var sse = SumSquaredErrors + other.SumSquaredErrors + d2 * Count * other.Count / n;        

        return new SampleStatistics(n, Sum + other.Sum, sse);
    }
    
    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the removal of multiple <paramref name="values"/>
    /// </summary>
    /// <param name="values">Values to remove from the sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Remove(IEnumerable<double> values) => AddOrRemoveMultiple(values, false);

    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the removal of multiple <paramref name="values"/>
    /// </summary>
    /// <param name="values">Values to remove from the sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Remove(params double[] values) => AddOrRemoveMultiple(values, false);

    
    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the addition of multiple <paramref name="values"/>
    /// </summary>
    /// <param name="values">Values to add to the sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Add(IEnumerable<double> values) => AddOrRemoveMultiple(values, true);

    /// <summary>
    /// Creates a new <see cref="SampleStatistics"/> with the addition of multiple <paramref name="values"/>
    /// </summary>
    /// <param name="values">Values to add to the sample</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Add(params double[] values) => AddOrRemoveMultiple(values, true);

    /// <summary>
    /// Multiplies every value in the sample by a <paramref name="multiplier"/> and returns a new <see cref="SampleStatistics"/>
    /// </summary>
    /// <param name="multiplier">Multiplier</param>
    /// <returns>A new <see cref="SampleStatistics"/></returns>
    [Pure]
    public SampleStatistics Multiply(double multiplier)
    {
        if (IsEmpty) return Empty;
        
        return new SampleStatistics(Count, Sum * multiplier, SumSquaredErrors * multiplier * multiplier);
    }
    
    #endregion
    
    #region Private functions

    private static (long N, double Sum, double SSE) Remove(double value, long count, long curN, double curSum, double curSse)
    {
        var newCount = curN - count;

        if (newCount < 0L) throw new InvalidOperationException();

        var curMean = curSum / curN;
        var newSum = curSum - value * count;
        var newMean = newSum / newCount;
        var newSse = curSse - (value - curMean) * (value - newMean) * count;

        return (newCount, newSum, newSse);
    }
    
    private static (long N, double Sum, double SSE) Add(double value, long count, long curN, double curSum, double curSse)
    {
        if (curN == 0L) return (count, count * value, 0d);
        
        var n = curN + count;

        var curMean = curSum / curN;
        var delta = value - curMean;
        
        var sum = curSum + (value * count);
        var mean = curMean + (delta / n) * count;
        var sse = curSse + delta * delta * curN / n;

        return (n, sum, sse);
    }
    
    private SampleStatistics AddOrRemoveMultiple(IEnumerable<double> values, bool add)
    {
        var n = Count;
        var sum = Sum;
        var sse = SumSquaredErrors;

        foreach (var x in values)
            (n, sum, sse) = add ? Add(x, 1L, n, sum, sse) : Remove(x, 1L, n, sum, sse);

        return new SampleStatistics(n, sum, sse);
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