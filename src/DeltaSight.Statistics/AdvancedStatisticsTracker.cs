using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using DeltaSight.Statistics.Abstractions;

namespace DeltaSight.Statistics;

/// <summary>
/// Tracks the 'advanced' statistics of a value sample such as the Greatest Common Divisor and a frequency diagram
/// </summary>
[Serializable]
public class AdvancedStatisticsTracker : StatisticsTracker<AdvancedStatistics>
{
    #region Private fields
    
    private SortedDictionary<double, long>? _frequencies;
    private SortedDictionary<long, long>? _integerMultipliers;
    private SimpleStatisticsTracker? _simple;

    #endregion

    #region Properties

    /// <summary>
    /// Multiplier that converts all values to integers
    /// <remarks>It equals <c>10 ^ d</c> where `d` is the maximum of decimal places of any included value</remarks>
    /// </summary>
    [JsonPropertyName("Multiplier")]
    public long IntegerMultiplier { get; private set; } = 1L;

    /// <summary>
    /// Greatest common divisor (scaled up to an integer number)
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("GCD")]
    public long? GreatestCommonDivisor { get; private set; }

    /// <summary>
    /// Frequency diagram for the included values
    /// </summary>
    [JsonInclude]
    public IReadOnlyDictionary<double, long>? Frequencies
    {
        get => _frequencies;
        private set => _frequencies = value is null ? null : new SortedDictionary<double, long>((IDictionary<double, long>) value);
    }

    /// <summary>
    /// Frequency diagram for the included integer multipliers
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Multipliers")]
    public IReadOnlyDictionary<long, long>? IntegerMultipliers
    {
        get  => _integerMultipliers;
        private set => _integerMultipliers = value is null ? null : new SortedDictionary<long, long>((IDictionary<long, long>) value);
    }

    [JsonInclude]
    [JsonHandleAs(typeof(SimpleStatisticsTracker))]
    public IReadOnlyStatisticsTracker<SimpleStatistics>? Simple
    {
        get => _simple;
        private set => _simple = (SimpleStatisticsTracker?) value;
    }

    #endregion
    
    #region Constructors

    public AdvancedStatisticsTracker() : base(0L, 0L)
    {
    }

    public static AdvancedStatisticsTracker From(params double[] values) => new(values);
    
    public AdvancedStatisticsTracker (IEnumerable<double> values) : base(values)
    {
    }
    
    private AdvancedStatisticsTracker(AdvancedStatisticsTracker other) : base(other)
    {
        if (other.IsEmpty()) return;
        
        _simple = new SimpleStatisticsTracker(other._simple);
        _frequencies = new SortedDictionary<double, long>(other._frequencies);
        _integerMultipliers = new SortedDictionary<long, long>(other._integerMultipliers);
        
        GreatestCommonDivisor = other.GreatestCommonDivisor;
        IntegerMultiplier = other.IntegerMultiplier;
    }

    #endregion

    
    #region Overrides

    [MemberNotNullWhen(false, nameof(_simple), nameof(_frequencies), nameof(_integerMultipliers))]
    public override bool IsEmpty() => base.IsEmpty();

    public override StatisticsTracker<AdvancedStatistics> Copy()
    {
        return new AdvancedStatisticsTracker(this);
    }

    public override AdvancedStatistics TakeSnapshot()
    {
        if (_simple is null || _frequencies is null || GreatestCommonDivisor is null)
            return AdvancedStatistics.Empty;

        var simpleSnap = _simple.TakeSnapshot();

        return new AdvancedStatistics
        {
            Maximum = _frequencies.Keys.Last(),
            Minimum = _frequencies.Keys.First(),
            Probabilities = _frequencies.ToImmutableDictionary(
                x => x.Key,
                x => 1d * x.Value / simpleSnap.Count),
            GreatestCommonDivisor = 1d * GreatestCommonDivisor.Value / IntegerMultiplier,
            Count = simpleSnap.Count,
            CountZero = simpleSnap.CountZero,
            Mean = simpleSnap.Mean,
            Sum = simpleSnap.Sum,
            Variance = simpleSnap.Variance,
            PopulationVariance = simpleSnap.PopulationVariance,
            StandardDeviation = simpleSnap.StandardDeviation,
            PopulationStandardDeviation = simpleSnap.PopulationStandardDeviation,
            SumSquaredError = simpleSnap.SumSquaredError
        };
    }

    protected override AdvancedStatisticsTracker MultiplyCore(double multiplier)
    {
        if (_frequencies is null) return new AdvancedStatisticsTracker();

        var tracker = new AdvancedStatisticsTracker();

        foreach (var (key, count) in _frequencies)
        {
            tracker.Add(key * multiplier, count);
        }

        return tracker;
    }

        
    protected override bool EqualsCore(StatisticsTracker<AdvancedStatistics> other)
    {
        if (other is not AdvancedStatisticsTracker ast) return false;

        return ast._frequencies.ContentEquals(_frequencies)
               && ast._integerMultipliers.ContentEquals(_integerMultipliers)
               && ast._simple!.Equals(_simple);
    }
    
    protected override void AddCore(StatisticsTracker<AdvancedStatistics> other)
    {
        if (other is not AdvancedStatisticsTracker ast) throw new InvalidOperationException();

        if (other.Count == 0) return;
        
        Add(ast.Frequencies);
    }

    protected override void ClearCore()
    {
        _simple!.Clear();
        _integerMultipliers!.Clear();
        _frequencies!.Clear();
        IntegerMultiplier = 1L;
        GreatestCommonDivisor = null;
    }
    
    protected override void RemoveCore(double value, long count)
    {
        _simple!.Remove(value, count);
        RemoveFromFrequencies(value, count);
        RemoveFromGCD(value, count);
    }

    private static long ComputeIntegerScale(double value) => (long)Math.Pow(10, GetDecimalPlaces((decimal)value, 4));

    
    protected override void AddCore(double value, long count)
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
            // New value so update int multiplier
            if (value != 0d)
            {
                _integerMultipliers ??= new SortedDictionary<long, long>();

                var myIntegerScale = ComputeIntegerScale(value);

                if (!_integerMultipliers.TryAdd(myIntegerScale, count))
                {
                    _integerMultipliers[myIntegerScale] += count;
                }

                if (myIntegerScale > IntegerMultiplier)
                {
                    GreatestCommonDivisor *= myIntegerScale / IntegerMultiplier; // Scale up GCD
                    IntegerMultiplier = myIntegerScale;
                }
            }
            
            var intValue = (long)Math.Round(value * IntegerMultiplier);

            GreatestCommonDivisor = GreatestCommonDivisor is null ? intValue : ComputeGreatestCommonDivisor(intValue, GreatestCommonDivisor.Value);
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
            if (myIntegerScale == IntegerMultiplier)
            {
                IntegerMultiplier = _integerMultipliers.Keys.Any()
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