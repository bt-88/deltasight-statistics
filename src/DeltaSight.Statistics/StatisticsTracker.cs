using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DeltaSight.Statistics.Abstractions;

namespace DeltaSight.Statistics;

public abstract class StatisticsTracker<T> : IStatisticsTracker<T>
    where T : IStatisticsSnapshot
{
    #region Properties

    [JsonInclude]
    [JsonPropertyName("N")]
    public long Count { get; private set; }

    [JsonInclude]
    [JsonPropertyName("N0")]
    public long CountZero { get; private set; }

    #endregion
    
    #region Constructors

    protected StatisticsTracker(StatisticsTracker<T> tracker) : this(tracker.Count, tracker.CountZero)
    {
    }

    protected StatisticsTracker(long count, long countZero)
    {
        Count = count;
        CountZero = countZero;
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

    #region Abstract members

    public abstract StatisticsTracker<T> Copy();
    protected abstract StatisticsTracker<T> CombineCore(StatisticsTracker<T> other);
    protected abstract StatisticsTracker<T> MultiplyCore(double multiplier);
    protected abstract void RemoveCore(double value, long count);
    protected abstract void AddCore(double value, long count);
    protected abstract void ClearCore();
    protected abstract T? TakeSnapshotCore();
    protected abstract bool EqualsCore(StatisticsTracker<T> other);

    #endregion

    #region Public members

    /// <summary>
    /// Creates a snapshot of the statistical descriptors based on the current tracker state
    /// </summary>
    /// <returns>A new snapshot</returns>
    public T? TakeSnapshot()
    {
        if (IsEmpty()) return default;

        return TakeSnapshotCore();
    }
    
    /// <summary>
    /// Tries to take a snapshot of the current tracked statistical descriptors
    /// </summary>
    /// <param name="snapshot">Snapshot of the current tracked statistical descriptors</param>
    /// <returns>True if a snapshot was taken, false otherwise</returns>
    public bool TryTakeSnapshot([NotNullWhen(true)] out T? snapshot)
    {
        snapshot = default;
        
        if (IsEmpty()) return false;

        snapshot = TakeSnapshotCore();

        return snapshot is not null;
    }
    
    /// <summary>
    /// Combines the current tracked sample with another tracked sample and returns a new instance
    /// </summary>
    /// <param name="other">Other tracked sample</param>
    public StatisticsTracker<T> Combine(StatisticsTracker<T>? other)
    {
        if (other is null) return Copy();

        return CombineCore(other);
    }

    /// <summary>
    /// Multiplies all values in the sample by a constant and returns a new instance
    /// </summary>
    /// <param name="multiplier">Multiplier</param>
    /// <exception cref="StatisticsTrackerException"></exception>
    public StatisticsTracker<T> Multiply(double multiplier)
    {
        if (multiplier == 0d)
            throw new StatisticsTrackerException($"An error occured while attempting {nameof(Multiply)}", new ArgumentOutOfRangeException(nameof(multiplier), $"{nameof(multiplier)} cannot be zero"));

        return MultiplyCore(multiplier);
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

            Count -= count;

            if (value != 0d) return;
            
            if (count > CountZero)
                throw new ArgumentOutOfRangeException(nameof(count),
                    $"{nameof(count)} ({count}) is greater than the existing {nameof(CountZero)} ({CountZero})");
                
            CountZero -= count;
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

            if (value != 0d) return;

            CountZero += count;
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