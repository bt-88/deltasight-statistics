using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace DeltaSight.Statistics;

/// <summary>
/// Tracks statistical descriptors for a sample of values
/// </summary>
[Serializable]
public class SimpleStatisticsTracker : IStatisticsTracker<SimpleStatistics>
{
    #region Equality

    
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not SimpleStatisticsTracker sst) return false;

        return Equals(sst);
    }
    

    protected bool Equals(SimpleStatisticsTracker other)
    {
        return Count == other.Count && Sum.Equals(other.Sum) && SumErrorSquared.Equals(other.SumErrorSquared);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Count, Sum, SumErrorSquared);
    }

    public static int GetHashCode(SimpleStatisticsTracker obj)
    {
        return HashCode.Combine(obj.Count, obj.Sum, obj.SumErrorSquared);
    }

    #endregion

    #region Constructors
    
    /// <summary>
    /// Creates an empty tracker
    /// </summary>
    public SimpleStatisticsTracker()
    {
    }
    

    public SimpleStatisticsTracker(params double[] values) : this(values as IEnumerable<double>)
    {
        
    }
    
    public SimpleStatisticsTracker(IEnumerable<double> values)
    {
        foreach (var value in values) Add(value);
    }
    
    /// <summary>
    /// Creates a copy of an <paramref name="other"/> tracker
    /// </summary>
    /// <param name="other">An other accumulator</param>
    public SimpleStatisticsTracker(SimpleStatisticsTracker other)
    {
        Count = other.Count;
        Sum = other.Sum;
        SumErrorSquared = other.SumErrorSquared;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Number of values in the sample
    /// </summary>
    [JsonInclude] [JsonPropertyName("N")] public long Count { get; private set; }

    /// <summary>
    /// Sum of the values in the sample
    /// </summary>
    [JsonInclude] public double Sum { get; private set; }

    /// <summary>
    /// Sum of Squared Errors (SSE)
    /// </summary>
    [JsonInclude] [JsonPropertyName("SSE")] public double SumErrorSquared { get; private set; }
    
    #endregion

    public bool IsEmpty() => Count == 0L;
    
    #region IStatisticsTracker implementations

    public void Remove(IEnumerable<double> values)
    {
        foreach(var value in values) Remove(value);
    }

    public void Clear()
    {
        Count = 0L;
        Sum = 0d;
        SumErrorSquared = 0d;
    }

    /// <summary>
    /// Creates a snapshot of the tracked statistics
    /// </summary>
    /// <returns>Null of the tracker is empty, or a set of statistics otherwise</returns>
    public SimpleStatistics? TakeSnapshot()
    {
        if (Count == 0L) return null;

        var variance = Count > 1L ? SumErrorSquared / (Count - 1L) : 0d;
        var popVariance = SumErrorSquared / Count;
        
        var stDev = Math.Sqrt(variance);
        var popStDev = Math.Sqrt(popVariance);

        var mean = Sum / Count;

        return new SimpleStatistics
        {
            Count = Count,
            Mean = mean,
            Variance = variance,
            PopulationVariance = popVariance,
            StandardDeviation = stDev,
            PopulationStandardDeviation = popStDev,
            Sum = Sum,
            SumSquaredError = SumErrorSquared
        };
    }
    
    /// <summary>
    /// Adds a <param name="value"/> to the sample
    /// </summary>
    /// <param name="value">Value to add</param>
    /// <param name="count">Number of times to add the value to the sample</param>
    /// <exception cref="ArgumentOutOfRangeException">If count is less than 1</exception>
    public void Add(double value, long count = 1L)
    {
        if (count < 1L) throw new ArgumentOutOfRangeException(nameof(count));

        if (Count == 0L)
        {
            Count = count;
            Sum = count * value;
            return;
        }

        var newCount = Count + count;
        var curMean = Sum / Count;
        var newSum = Sum + value * count;
        var newMean = newSum / newCount;
        var newSse = SumErrorSquared + (value - curMean) * (value - newMean) * count;

        SumErrorSquared = newSse;
        Count = newCount;
        Sum = newSum;
    }

    public void Add(params double[] values) => Add(values as IEnumerable<double>);

    public void Add(IEnumerable<double> values)
    {
        foreach (var value in values) Add(value);
    }

    /// <summary>
    /// Removes a <paramref name="value"/> from the sample
    /// </summary>
    /// <param name="value">Value to remove</param>
    /// <param name="count">Number of times to remove the value</param>
    /// <exception cref="StatisticsTrackerException">If the sample is empty or if the count to be removed is greater than the sample count</exception>
    public void Remove(double value, long count = 1L)
    {
        if (count < 1L) throw new StatisticsTrackerException("Could not perform Remove", new ArgumentOutOfRangeException(nameof(count)));

        var newCount = Count - count;
        
        switch (Count - count)
        {
            case < 0L:
                throw new StatisticsTrackerException("Could not perform Remove", new ArgumentOutOfRangeException(nameof(count), $"Count ({nameof(count)}) is greater than added count ({Count})"));
            case 1L:
                Sum = count * value;
                Count = 1L;
                SumErrorSquared = 0d;
                return;
            case 0L:
                Sum = 0d;
                Count = 0L;
                SumErrorSquared = 0d;
                return;
        }

        var curMean = Sum / Count;
        var newSum = Sum - value * count;
        var newMean = newSum / newCount;
        var newSse = SumErrorSquared - (value - curMean) * (value - newMean) * count;

        SumErrorSquared = newSse;
        Count = newCount;
        Sum = newSum;
    }

    public void Remove(params double[] values) => Remove(values as IEnumerable<double>);

    /// <summary>
    /// Combines the current tracker with another <see cref="SimpleStatisticsTracker"/>
    /// </summary>
    /// <param name="value">Another tracker</param>
    /// <returns>A new <see cref="SimpleStatisticsTracker"/></returns>
    public IStatisticsTracker<SimpleStatistics> Add(IStatisticsTracker<SimpleStatistics> value)
    {
        if (value is not SimpleStatisticsTracker other) throw new InvalidOperationException();
        
        var n = Count + other.Count;

        if (n == 0L) return new SimpleStatisticsTracker(); 
        if (Count == 0L) return new SimpleStatisticsTracker(other);
        if (other.Count == 0L) return new SimpleStatisticsTracker(this);
        
        var d = Sum / Count - other.Sum / other.Count;
        var d2 = d * d;
        var sse = SumErrorSquared + other.SumErrorSquared + d2 * Count * other.Count / n;

        return new SimpleStatisticsTracker(n, Sum + other.Sum, sse);
    }

    /// <summary>
    /// Multiplies the current tracker X by m to create a new <see cref="SimpleStatisticsTracker"/> of m*X
    /// </summary>
    /// <param name="multiplier">Multiplier</param>
    /// <returns>A new <see cref="SimpleStatisticsTracker"/></returns>
    public IStatisticsTracker<SimpleStatistics> Multiply(double multiplier)
    {
        if (Count == 0L) return new SimpleStatisticsTracker();

        return new SimpleStatisticsTracker(Count, Sum * multiplier, SumErrorSquared * multiplier * multiplier);
    }

    #endregion
    
    #region Operators
    
    [return: NotNullIfNotNull("a")]
    [return: NotNullIfNotNull("b")]
    public static SimpleStatisticsTracker? operator +(
        SimpleStatisticsTracker? a,
        SimpleStatisticsTracker? b)
    {
        if (a is null && b is null) return null;
        if (b is null) return new SimpleStatisticsTracker(a!);
        if (a is null) return new SimpleStatisticsTracker(b);
        
        return (SimpleStatisticsTracker)a.Add(b);
    }
    
    #endregion
}