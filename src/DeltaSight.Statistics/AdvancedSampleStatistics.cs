using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace DeltaSight.Statistics;

/// <summary>
/// Tracks the 'advanced' statistics of a value sample such as the Greatest Common Divisor and a frequency diagram
/// </summary>
[Serializable]
public class AdvancedStatisticsTracker : IStatisticsTracker<AdvancedStatistics>, IStatisticsTracker
{
    #region Private fields
    
    private SortedDictionary<double, long>? _frequencies;
    private SortedDictionary<long, long>? _integerMultipliers;
    private SimpleStatisticsTracker? _simple;
    private long? _gcd;
    private long _integerMultiplier = 1L;
    
    #endregion

    public override bool Equals(object? obj)
    {
        if (obj is not AdvancedStatisticsTracker ast) return false;

        if (IsEmpty() && ast.IsEmpty()) return true;
        if (IsEmpty() || ast.IsEmpty()) return false;

        return ast._frequencies.ContentEquals(_frequencies)
               && ast._integerMultipliers.ContentEquals(_integerMultipliers)
               && ast._simple.Equals(_simple);
    }

    #region Properties

    /// <summary>
    /// Multiplier that converts all values to integers
    /// <remarks>It equals <c>10 ^ d</c> where `d` is the maximum of decimal places of any included value</remarks>
    /// </summary>
    public long IntegerMultiplier => _integerMultiplier;

    /// <summary>
    /// Greatest common divisor (scaled up to an integer number)
    /// </summary>
    [JsonInclude]
    public long? GreatestCommonDivisor => _gcd;

    /// <summary>
    /// Frequency diagram for the included values
    /// </summary>
    [JsonInclude]
    public IReadOnlyDictionary<double, long>? Frequencies => _frequencies;

    /// <summary>
    /// Frequency diagram for the included integer multipliers
    /// </summary>
    [JsonInclude]
    public IReadOnlyDictionary<long, long>? IntegerMultipliers => _integerMultipliers;

    [JsonInclude] public IReadOnlyStatisticsTracker<SimpleStatistics>? Simple => _simple;

    #endregion
    
    #region Constructors

    /// <summary>
    /// Creates an empty advanced statistical descriptors tracker
    /// </summary>
    public AdvancedStatisticsTracker()
    {
    }

    [JsonConstructor]
    public AdvancedStatisticsTracker(SortedDictionary<long, long>? integerMultipliers, long greatestCommonDivisor,
        SortedDictionary<double, long>? frequencies, SimpleStatisticsTracker? simple)
    {
        _integerMultipliers =  integerMultipliers;
        _simple = simple;
    }

    private AdvancedStatisticsTracker(AdvancedStatisticsTracker other)
    {
        if (other.IsEmpty()) return;
        
        _simple = new SimpleStatisticsTracker(other._simple);
        _frequencies = new SortedDictionary<double, long>(other._frequencies);
        _integerMultipliers = new SortedDictionary<long, long>(other._integerMultipliers);
        _gcd = other._gcd;
        _integerMultiplier = other._integerMultiplier;
    }

    /// <summary>
    /// Creates a new advanced statistical descriptors tracker and adds a collection of values to it
    /// </summary>
    public AdvancedStatisticsTracker(IEnumerable<double> values)
    {
        foreach (var value in values) Add(value);
    }

    /// <summary>
    /// Creates a new advanced statistical descriptors tracker and adds a collection of values to it
    /// </summary>
    public AdvancedStatisticsTracker(params double[] values) : this(values as IEnumerable<double>)
    {
    }

    /// <summary>
    /// Creates a new advanced statistical descriptors tracker and adds a collection of values to it
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

    IStatisticsSnapshot? IReadOnlyStatisticsTracker<IStatisticsSnapshot>.TakeSnapshot()
    {
        return TakeSnapshot();
    }

    [MemberNotNullWhen(false, nameof(_simple), nameof(_frequencies), nameof(_integerMultipliers))]
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

    public AdvancedStatisticsTracker Multiply(double multiplier)
    {
        if (_frequencies is null) return new AdvancedStatisticsTracker();

        var tracker = new AdvancedStatisticsTracker();

        foreach (var (key, count) in _frequencies)
        {
            tracker.Add(key * multiplier, count);
        }

        return tracker;
    }

    public AdvancedStatisticsTracker Combine(AdvancedStatisticsTracker other)
    {
        var tracker = new AdvancedStatisticsTracker();
        
        tracker.Add(_frequencies);
        tracker.Add(other._frequencies);

        return tracker;
    }
    
    public void Clear()
    {
        _simple?.Clear();
        _integerMultipliers?.Clear();
        _frequencies?.Clear();
        _integerMultiplier = 1L;
        _gcd = null;
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
            if (_simple is null) throw new NullReferenceException();
            
            RemoveFromFrequencies(value, count);

            if (_frequencies!.Count == 0)
            {
                // Clear entire state if frequency diagram is now empty
                Clear();
                return;
            }
            
            RemoveFromGCD(value, count);
            _simple.Remove(value, count);
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

                if (myIntegerScale > _integerMultiplier)
                {
                    _gcd = myIntegerScale * _gcd / _integerMultiplier; // Scale up GCD
                    _integerMultiplier = myIntegerScale;
                }
            }
            
            var intValue = (long)Math.Round(value * _integerMultiplier);

            _gcd = _gcd is null ? intValue : ComputeGreatestCommonDivisor(intValue, _gcd.Value);
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

            return;
        }

        // Update entry
        _frequencies[value] -= count;
    }
    
    private void RemoveFromGCD(double value, long count)
    {
        if (value == 0d) return;
        
        var myIntegerScale = ComputeIntegerScale(value);
        var myIntegerScaleCount = _integerMultipliers![myIntegerScale];

        if (myIntegerScaleCount < count) throw new ArgumentOutOfRangeException(nameof(count), $"Count ({count}) is higher than added integer scale count ({myIntegerScaleCount})");

        if (myIntegerScaleCount == count)
        {
            // Remove integer scale
            _integerMultipliers.Remove(myIntegerScale);

            // Only Adjust GCD and IntegerScale if required
            if (myIntegerScale == _integerMultiplier)
            {
                _integerMultiplier = _integerMultipliers.Keys.Any()
                    ? _integerMultipliers.Keys.Last()
                    : 1L; // Update Integer multiplier
            }
        }
        else
        {
            // Only update integer scale count
            _integerMultipliers[myIntegerScale] -= count;
        }

        // Update GCD
        var newGcd = (long)Math.Round(_frequencies!.Keys.First() * _integerMultiplier);

        foreach (var val in _frequencies.Keys.Skip(1))
        {
            if (newGcd == 1) break;

            newGcd = ComputeGreatestCommonDivisor(newGcd, (long)Math.Round(val * _integerMultiplier));
        }

        _gcd = newGcd;
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
    
    #region Operators
    
    [return: NotNullIfNotNull("a")]
    [return: NotNullIfNotNull("b")]
    public static AdvancedStatisticsTracker? operator +(
        AdvancedStatisticsTracker? a,
        AdvancedStatisticsTracker? b)
    {
        if (a is null && b is null) return null;
        if (b is null) return new AdvancedStatisticsTracker(a!);
        if (a is null) return new AdvancedStatisticsTracker(b);
        
        return a.Combine(b);
    }

    [return: NotNullIfNotNull("a")]
    public static AdvancedStatisticsTracker? operator *(
        AdvancedStatisticsTracker? a,
        double multiplier)
    {
        if (a is null) return null;
        return a.Multiply(multiplier);
    }

    #endregion
}