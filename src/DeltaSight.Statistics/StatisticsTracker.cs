using System.Text.Json.Serialization;
using DeltaSight.Statistics.Abstractions;

namespace DeltaSight.Statistics;

public abstract class StatisticsTracker<T> : IStatisticsTracker<T>
    where T : IStatisticsSnapshot
{
    #region Properties

    [JsonInclude]
    [JsonPropertyName("N")]
    public long Count { get; protected set; }

    [JsonInclude]
    [JsonPropertyName("N0")]
    public long CountZero { get; protected set; }
    
    [JsonInclude]
    public double Sum { get; protected set; }
    
    #endregion
    
    #region Constructors

    protected StatisticsTracker(StatisticsTracker<T> tracker) : this(tracker.Count, tracker.CountZero, tracker.Sum)
    {
    }

    protected StatisticsTracker() : this(0L, 0L, 0d)
    {
        
    }
    
    protected StatisticsTracker(long count, long countZero, double sum)
    {
        Count = count;
        CountZero = countZero;
        Sum = sum;
    }

    /// <summary>
    /// Creates a new advanced statistical descriptors tracker and adds a collection of values to it
    /// </summary>
    protected StatisticsTracker(IEnumerable<double> values)
    {
        foreach (var value in values) Add(value);
    }

    /// <summary>
    /// Creates a new advanced statistical descriptors tracker and adds a collection of values to it
    /// </summary>
    /// <param name="valueCounts"></param>
    protected StatisticsTracker(IEnumerable<KeyValuePair<double, long>> valueCounts)
    {
        foreach (var (value, count) in valueCounts) Add(value, count);
    }
    
    #endregion
    
    #region Protected members
    
    protected void AddInner(StatisticsTracker<T> tracker)
    {
        Count += tracker.Count;
        CountZero += tracker.CountZero;
        Sum += tracker.Sum;
    }
    
    #endregion

    #region Abstract members

    public abstract StatisticsTracker<T> Copy();
    //protected abstract void RemoveCore(double value, long count);
    protected abstract void AddCore(double value, long count);
    protected abstract void AddCore(StatisticsTracker<T> other);
    protected abstract void ClearCore();
    /// <summary>
    /// Creates a snapshot of the statistical descriptors based on the current tracker state
    /// </summary>
    /// <returns>A new snapshot</returns>
    public abstract T TakeSnapshot();
    protected abstract bool EqualsCore(StatisticsTracker<T> other);

    #endregion

    #region Public members

    public void Add(StatisticsTracker<T>? other)
    {
        if (other is null) return;
        
        AddCore(other);
    }

    /// <summary>
    /// Combines the current tracked sample with another tracked sample and returns a new instance
    /// </summary>
    /// <param name="other">Other tracked sample</param>
    public StatisticsTracker<T> Combine(StatisticsTracker<T>? other)
    {
        var combined = Copy();

        if (other is null) return combined;

        combined.AddCore(other);

        return combined;
    }

    /// <summary>
    /// Empties the tracker
    /// </summary>
    public void Clear()
    {
        Count = 0L;
        CountZero = 0L;
        
        ClearCore();
    }

    /// <summary>
    /// Adds a value to the sample, one or more times
    /// </summary>
    /// <param name="value">Value to be added</param>
    /// <param name="count">Number of observations of that value</param>
    public void Add(double value, long count = 1L)
    {
        try
        {
            AddCore(value, count);
            
            Count += count;

            if (value == 0d)
            {
                CountZero += count;
            }
            else
            {
                Sum += value * count;
            }
        }
        catch (Exception e)
        {
            throw new StatisticsTrackerException(
                $"An error occured while attempting to perform {nameof(Add)} for value '{value}' with count '{count}'", e);
        }
    }
    
    /// <summary>
    /// Adds a <see cref="IDictionary{TKey, TValue}"/> to the sample
    /// </summary>
    /// <param name="hist">A collection of values and corresponding observation counts</param>
    public void Add(IEnumerable<KeyValuePair<double, long>>? hist)
    {
        if (hist is null) return;
        
        foreach(var (quantity, count) in hist) Add(quantity, count);
    }
    
    /// <summary>
    /// Adds a <see cref="IDictionary{TKey, TValue}"/> to the sample
    /// </summary>
    /// <param name="hist">A collection of values and corresponding observation counts</param>
    public void Add(IEnumerable<KeyValuePair<double, int>>? hist)
    {
        if (hist is null) return;
        
        foreach(var (quantity, count) in hist) Add(quantity, count);
    }

    /// <summary>
    /// Adds values to the sample
    /// </summary>
    /// <param name="values">Value source</param>
    public void Add(IEnumerable<double> values)
    {
        foreach(var value in values) Add(value);
    }

    #endregion
    
    public virtual bool IsEmpty() => Count == 0L;

    public override bool Equals(object? obj)
    {
        if (obj is not StatisticsTracker<T> st) return false;

        if (IsEmpty() && st.IsEmpty()) return true;
        if (IsEmpty() || st.IsEmpty()) return false;

        return EqualsCore(st);
    }
}

public abstract class StatisticsTrackerWithRemove<T> : StatisticsTracker<T>, IStatisticsTrackerWithRemove<T>
    where T : IStatisticsSnapshot
{
    protected StatisticsTrackerWithRemove(long count, long countZero, double sum) : base(count, countZero, sum)
    {
    }

    protected StatisticsTrackerWithRemove(IEnumerable<double> values) : base(values)
    {
    }

    protected StatisticsTrackerWithRemove(IEnumerable<KeyValuePair<double, long>> valueCounts) : base(valueCounts)
    {
    }

    protected StatisticsTrackerWithRemove()
    {
    }

    protected StatisticsTrackerWithRemove(StatisticsTracker<T> tracker) : base(tracker)
    {
    }

    /// <summary>
    /// Removes a <paramref name="value"/> from the sample, one or more times 
    /// </summary>
    /// <param name="value">The value to be removed from the population</param>
    /// <param name="count">The number of observations of that value to be removed</param>
    /// <exception cref="StatisticsTrackerException"></exception>
    public void Remove(double value, long count = 1L)
    {
        try
        {
            if (count > Count)
                throw new ArgumentOutOfRangeException(nameof(count),
                    $"{nameof(count)} ({count}) is greater than the existing {nameof(Count)} ({Count})");

            if (count == Count)
            {
                Clear();
                return;
            }
            
            RemoveCore(value, count);

            if (value == 0d)
            {
                if (count > CountZero)
                    throw new ArgumentOutOfRangeException(nameof(count),
                        $"{nameof(count)} ({count}) is greater than the existing {nameof(CountZero)} ({CountZero})");

                CountZero -= count;
            }
            else
            {
                Sum -= count * value;
            }
            
            Count -= count;
        }
        catch (Exception e)
        {
            throw new StatisticsTrackerException(
                $"An error occured while attempting to perform {nameof(Remove)} for value '{value}' with count '{count}'", e);
        }
    }

    public void Remove(IEnumerable<double> values)
    {
        foreach (var value in values) Remove(value);
    }

    protected abstract void RemoveCore(double value, long count);
}