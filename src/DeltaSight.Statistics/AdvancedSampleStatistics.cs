using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeltaSight.Statistics;

/// <summary>
/// Tracks the 'advanced' statistics of a value sample such as the Greatest Common Divisor and a frequency diagram
/// </summary>
[Serializable]
public class AdvancedStatisticsTracker : IStatisticsTracker<AdvancedStatistics>
{
    [JsonPropertyName("Frequencies")]
    [JsonInclude]
    private SortedDictionary<double, long>? _frequencies;
    
    [JsonPropertyName("IntegerMultipliers")]
    [JsonInclude]
    private SortedDictionary<long, long>? _integerMultipliers;

    [JsonPropertyName("Simple")]
    [JsonInclude]
    private SimpleStatisticsTracker? _simple;

    #region Properties

    /// <summary>
    /// Multiplier that converts all values to integers
    /// <remarks>It equals <c>10 ^ d</c> where `d` is the maximum of decimal places of any included value</remarks>
    /// </summary>
    [JsonInclude]
    public long IntegerMultiplier { get; private set; } = 1L;

    /// <summary>
    /// Greatest common divisor (scaled up to an integer number)
    /// </summary>
    [JsonInclude]
    public long? GreatestCommonDivisor { get; private set; }
    
    #endregion
    
    #region Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="AdvancedStatisticsTracker"/>
    /// </summary>
    public AdvancedStatisticsTracker()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AdvancedStatisticsTracker"/> and adds a collection of values to it
    /// </summary>
    public AdvancedStatisticsTracker(IEnumerable<double> values)
    {
        foreach (var value in values) Add(value);
    }
    
    /// <summary>
    /// Initializes a new instance of <see cref="AdvancedStatisticsTracker"/> and adds a collection of value/count entries to it
    /// </summary>
    /// <param name="valueCounts"></param>
    public AdvancedStatisticsTracker(IEnumerable<KeyValuePair<double, long>> valueCounts)
    {
        foreach (var (value, count) in valueCounts) Add(value, count);
    }

    #endregion

    private static long ComputeIntegerScale(double value) => (long)Math.Pow(10, GetDecimalPlaces((decimal)value, 4));


    /// <summary>
    /// Adds a <see cref="IDictionary{TKey, TValue}"/> to the population
    /// </summary>
    /// <param name="hist">A collection of values and corresponding observation counts</param>
    public void Add(IEnumerable<KeyValuePair<double, long>>? hist)
    {
        if (hist is null) return;
        
        foreach(var (quantity, count) in hist) Add(quantity, count);
    }
    
    /// <summary>
    /// Adds a <see cref="IDictionary{TKey, TValue}"/> to the population
    /// </summary>
    /// <param name="hist">A collection of values and corresponding observation counts</param>
    public void Add(IEnumerable<KeyValuePair<double, int>>? hist)
    {
        if (hist is null) return;
        
        foreach(var (quantity, count) in hist) Add(quantity, count);
    }

    #region IStatissTracker implementations

    public bool IsEmpty() => _simple?.IsEmpty() ?? true;
    
    public AdvancedStatistics? TakeSnapshot()
    {
        if (_simple is null || _frequencies is null || GreatestCommonDivisor is null) return null;

        var simpleSnap = _simple.TakeSnapshot();

        if (simpleSnap is null) return null;

        return new AdvancedStatistics
        {
            Maximum = _frequencies.Keys.Last(),
            Minimum = _frequencies.Keys.First(),
            Probabilities = _frequencies.ToImmutableDictionary(
                x => x.Key,
                x => 1d * x.Value / simpleSnap.Count),
            GreatestCommonDivisor = 1d * GreatestCommonDivisor.Value / IntegerMultiplier,
            Count = simpleSnap.Count,
            Mean = simpleSnap.Mean,
            Sum = simpleSnap.Sum,
            Variance = simpleSnap.Variance,
            PopulationVariance = simpleSnap.PopulationVariance,
            StandardDeviation = simpleSnap.StandardDeviation,
            PopulationStandardDeviation = simpleSnap.PopulationStandardDeviation,
            SumSquaredError = simpleSnap.SumSquaredError
        };
    }

    public IStatisticsTracker<AdvancedStatistics> Multiply(double multiplier)
    {
        if (_frequencies is null) return new AdvancedStatisticsTracker();

        var tracker = new AdvancedStatisticsTracker();

        foreach (var (key, count) in _frequencies)
        {
            tracker.Add(key * multiplier, count);
        }

        return tracker;
    }

    public IStatisticsTracker<AdvancedStatistics> Add(IStatisticsTracker<AdvancedStatistics> other)
    {
        var tracker = new AdvancedStatisticsTracker();
        
        tracker.Add(_frequencies);

        if (other is AdvancedStatisticsTracker ast)
        {
            tracker.Add(ast._frequencies);
        }

        return tracker;
    }
    
    public void Clear()
    {
        _simple?.Clear();
        _integerMultipliers?.Clear();
        _frequencies?.Clear();
        IntegerMultiplier = 1L;
        GreatestCommonDivisor = null;
    }
    
    /// <summary>
    /// Removes a <paramref name="value"/> from the population 
    /// </summary>
    /// <param name="value">The value to be removed from the population</param>
    /// <param name="count">The number of observations of that value to be removed</param>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void Remove(double value, long count = 1L)
    {
        try
        {
            _simple!.Remove(value, count);

            RemoveFromFrequencies(value, count);
            RemoveFromGCD(value, count);
        }
        catch (Exception ex)
        {
            throw new StatisticsTrackerException($"Could not remove value '{value}' with count '{count}'", ex);
        }
    }

    public void Remove(IEnumerable<double> values)
    {
        foreach(var value in values) Remove(value);
    }

    public void Remove(params double[] values) => Remove(values as IEnumerable<double>);
    
    public void Add(params double[] values) => Add(values as IEnumerable<double>);

    public void Add(IEnumerable<double> values)
    {
        foreach (var value in values) Add(value);
    }
    
    /// <summary>
    /// Adds a value to the population
    /// </summary>
    /// <param name="value">Value to be added</param>
    /// <param name="count">Number of observations of that value</param>
    public void Add(double value, long count = 1L)
    {
        _simple ??= new SimpleStatisticsTracker();
            
        _simple.Add(value, count);

        _frequencies ??= new SortedDictionary<double, long>();
        
        if (!_frequencies.TryAdd(value, count))
        {
            _frequencies[value] += count;
        }
        else
        {
            // Update int multiplier
            if (value != 0)
            {
                _integerMultipliers ??= new SortedDictionary<long, long>();

                var myIntegerScale = ComputeIntegerScale(value);

                if (!_integerMultipliers.TryAdd(myIntegerScale, count))
                {
                    _integerMultipliers[myIntegerScale] += count;
                }

                if (myIntegerScale > IntegerMultiplier)
                {
                    GreatestCommonDivisor = myIntegerScale * GreatestCommonDivisor / IntegerMultiplier; // Scale up GCD
                    IntegerMultiplier = myIntegerScale;
                }
            }
            
            var intValue = (long)Math.Round(value * IntegerMultiplier);

            GreatestCommonDivisor = GreatestCommonDivisor.HasValue ? ComputeGreatestCommonDivisor(intValue, GreatestCommonDivisor.Value) : intValue;
        }
    }
    
    #endregion

    #region Private methods
    
    private void RemoveFromFrequencies(double value, long count)
    {
        var currentCount = _frequencies![value];
        
        if (count > currentCount)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Count ({count}) is higher than the added count ({currentCount})");
        }

        if (count == currentCount)
        {
            // Remove entry from frequencies
            _frequencies.Remove(value);

            if (_frequencies.Count != 0) return;
            
            Clear(); // If frequencies is empty, then clear entire object
            
            return;
        }

        // Update entry
        _frequencies[value] -= count;
    }
    
    private void RemoveFromGCD(double value, long count)
    {
        if (value == 0) return;
        
        var myIntegerScale = ComputeIntegerScale(value);
        var myIntegerScaleCount = _integerMultipliers![myIntegerScale];

        if (myIntegerScaleCount < count) throw new ArgumentOutOfRangeException(nameof(count), $"Count ({count}) is higher than added integer scale count ({myIntegerScaleCount})");

        if (myIntegerScaleCount == count)
        {
            // Remove integer scale
            _integerMultipliers.Remove(myIntegerScale);

            // Only Adjust GCD and IntegerScale if required
            if (myIntegerScale == IntegerMultiplier)
            {
                IntegerMultiplier = _integerMultipliers.Count > 0 ? _integerMultipliers.Keys.Last() : 1; // Update Integer multiplier
            }
        }
        else
        {
            // Only update integer scale count
            _integerMultipliers[myIntegerScale] -= count;
        }

        // Update GCD
        var newGcd = (long)Math.Round(_frequencies!.Keys.First() * IntegerMultiplier);

        foreach (var val in _frequencies.Keys.Skip(1))
        {
            if (newGcd == 1) break;

            newGcd = ComputeGreatestCommonDivisor(newGcd, (long)Math.Round(val * IntegerMultiplier));
        }

        GreatestCommonDivisor = newGcd;
    }
    
    /// <summary>
    /// Computes the greatest common divisor (GCD) of two integers using Euclid's algorithm
    /// </summary>
    /// <param name="x">First integer value</param>
    /// <param name="y">Second integer value</param>
    /// <returns>Greatest common divisor of <paramref name="x"/> and <paramref name="y"/></returns>
    private static long ComputeGreatestCommonDivisor(long x, long y)
    {
        while (y != 0)
        {
            var remainder = x%y;
            x = y;
            y = remainder;
        }

        return Math.Abs(x);
    }

    /// <summary>
    /// Gets the number of decimals for a decimal value.
    /// </summary>
    /// <param name="n">The decimal number to check.</param>
    /// <param name="max">The maximum number of decimals to check.</param>
    /// <returns>The number of decimal places, maximized by <paramref name="max"/>.</returns>
    private static int GetDecimalPlaces(decimal n, int? max = default)
    {
        n = Math.Abs(n); //make sure it is positive.
        n -= (int)n;     //remove the integer part of the number.
        var places = 0;
        while (n > 0)
        {
            places++;

            if (places == max) break;

            n *= 10;
            n -= (int)n;
        }

        return places;
    }
    
    #endregion
}