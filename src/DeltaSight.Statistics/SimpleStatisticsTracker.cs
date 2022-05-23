using System.Text.Json.Serialization;

namespace DeltaSight.Statistics;

/// <summary>
/// Tracks statistical descriptors for a sample of values
/// </summary>
[Serializable]
public class SimpleStatisticsTracker : StatisticsTrackerWithRemove<SimpleStatistics>
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
    private SimpleStatisticsTracker(long n, long n0, double sum, double sse) : base(n, n0, sum)
    {
        SumSquaredError = sse;
    }
    
    /// <summary>
    /// Creates a copy of an <paramref name="other"/> tracker
    /// </summary>
    /// <param name="other">An other accumulator</param>
    public SimpleStatisticsTracker(SimpleStatisticsTracker other) : base(other)
    {
        SumSquaredError = other.SumSquaredError;
    }

    #endregion

    #region Properties

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
            return;
        }

        var newCount = Count + count;
        var curMean = Sum / Count;
        var newSum = Sum + value * count;
        var newMean = newSum / newCount;
        var newSse = SumSquaredError + (value - curMean) * (value - newMean) * count;

        SumSquaredError = newSse;
    }
    
    protected override void RemoveCore(double value, long count)
    {
        var newCount = Count - count;
        
        switch (newCount)
        {
            case < 0L:
                return;
            case <= 1L:
                SumSquaredError = 0d;
                return;
        }

        var curMean = Sum / Count;
        var newSum = Sum - value * count;
        var newMean = newSum / newCount;
        var newSse = SumSquaredError - (value - curMean) * (value - newMean) * count;

        SumSquaredError = newSse;
    }
    
    protected override void AddCore(StatisticsTracker<SimpleStatistics> other)
    {
        if (other is not SimpleStatisticsTracker sst) throw new InvalidOperationException();
        
        if (other.Count == 0L) return;

        var n = Count + other.Count;
        var sse = SumSquaredError + sst.SumSquaredError;

        if (Count > 0L)
        {
            var d = Sum / Count - sst.Sum / sst.Count;
            
            sse += d * d * Count * sst.Count / n;
        }
        
        AddInner(sst);
        
        SumSquaredError = sse;
    }

    public override SimpleStatisticsTracker Copy() => new (this);
    
    protected override bool EqualsCore(StatisticsTracker<SimpleStatistics> other)
    {
        if (other is not SimpleStatisticsTracker st) return false;
        
        return Sum.Equals(st.Sum)
               && SumSquaredError.Equals(st.SumSquaredError);
    }
    
    #endregion

}