using System.Text.Json.Serialization;

namespace DeltaSight.Statistics;

/// <summary>
/// Tracks statistical descriptors for a sample of values
/// </summary>
[Serializable]
public class SimpleStatisticsTracker : StatisticsTracker<SimpleStatistics>
{

    #region Constructors
    
    /// <summary>
    /// Creates an empty tracker for the 'simple' statistics of a running value sample
    /// </summary>
    public SimpleStatisticsTracker()
    {
    }

    public static SimpleStatisticsTracker From(params double[] values)
        => new(values);

    public SimpleStatisticsTracker(IEnumerable<double> values) : base(values)
    {
    }

    [JsonConstructor]
    private SimpleStatisticsTracker(long n, double nm, long n0, double sum, double sse) : base(n, nm, n0)
    {
        Sum = sum;
        SumSquaredError = sse;
    }
    
    /// <summary>
    /// Creates a copy of an <paramref name="other"/> tracker
    /// </summary>
    /// <param name="other">An other accumulator</param>
    public SimpleStatisticsTracker(SimpleStatisticsTracker other) : base(other)
    {
        Sum = other.Sum;
        SumSquaredError = other.SumSquaredError;
    }

    #endregion

    #region Properties
    
    /// <summary>
    /// Sum of the values in the sample
    /// </summary>
    [JsonInclude]
    public double Sum { get; private set; }

    /// <summary>
    /// Sum of Squared Errors (SSE)
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("SSE")]
    public double SumSquaredError { get; private set; }
    
    #endregion

    #region Overrides
    
    protected override void ClearCore()
    {
        Sum = 0d;
        SumSquaredError = 0d;
    }
    
    public override SimpleStatistics TakeSnapshot()
    {
        if (IsEmpty()) return SimpleStatistics.Empty;

        var variance = Count > 1L ? SumSquaredError / (Count - 1L) : 0d;
        var popVariance = Count > 0L ? SumSquaredError / Count : 0d;
        
        var stDev = Math.Sqrt(variance);
        var popStDev = Math.Sqrt(popVariance);

        var mean = Sum / Count;

        var cv = mean > 0d ? stDev / mean : 0d;
        var popCv = mean > 0d ? popStDev / mean : 0d;

        return new SimpleStatistics
        {
            CountZero = CountZero,
            Count = Count,
            CountMultiplied = CountMultiplied,
            Mean = mean,
            Variance = variance,
            PopulationVariance = popVariance,
            StandardDeviation = stDev,
            PopulationStandardDeviation = popStDev,
            Sum = Sum,
            SumSquaredError = SumSquaredError,
            CoefficientOfVariation = cv,
            PopulationCoefficientOfVariation = popCv
        };
    }
    
    protected override void AddCore(double value, long count)
    {
        if (Count == 0L)
        {
            Sum = count * value;
            return;
        }

        var newCount = Count + count;
        var curMean = Sum / Count;
        var newSum = Sum + value * count;
        var newMean = newSum / newCount;
        var newSse = SumSquaredError + (value - curMean) * (value - newMean) * count;

        SumSquaredError = newSse;
        Sum = newSum;
    }
    
    protected override void RemoveCore(double value, long count)
    {
        var newCount = Count - count;
        
        switch (newCount)
        {
            case < 0L:
                return;
            case 1L:
                Sum = count * value;
                SumSquaredError = 0d;
                return;
            case 0L:
                Sum = 0d;
                SumSquaredError = 0d;
                return;
        }

        var curMean = Sum / Count;
        var newSum = Sum - value * count;
        var newMean = newSum / newCount;
        var newSse = SumSquaredError - (value - curMean) * (value - newMean) * count;

        SumSquaredError = newSse;
        Sum = newSum;
    }
    
    protected override void AddCore(StatisticsTracker<SimpleStatistics> other)
    {
        if (other is not SimpleStatisticsTracker sst) throw new InvalidOperationException();
        
        if (other.Count == 0L) return;

        var n = Count + other.Count;
        var d = Sum / Count - sst.Sum / sst.Count;
        var d2 = d * d;
        var sse = SumSquaredError + sst.SumSquaredError + d2 * Count * sst.Count / n;

        Count += sst.Count;
        CountZero += sst.CountZero;
        CountMultiplied += sst.CountMultiplied;
        Sum += sst.Sum;
        SumSquaredError = sse;
    }

    public override SimpleStatisticsTracker Copy() => new (this);

    protected override SimpleStatisticsTracker MultiplyCore(double multiplier)
    {
        return Count == 0L
            ? new SimpleStatisticsTracker()
            : new SimpleStatisticsTracker(Count, CountMultiplied * multiplier, CountZero, Sum * multiplier, SumSquaredError * multiplier * multiplier);
    }

    protected override bool EqualsCore(StatisticsTracker<SimpleStatistics> other)
    {
        if (other is not SimpleStatisticsTracker st) return false;
        
        return Sum.Equals(st.Sum)
               && SumSquaredError.Equals(st.SumSquaredError);
    }
    
    #endregion

}