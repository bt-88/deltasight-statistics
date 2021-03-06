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
    /// Creates an empty tracker
    /// </summary>
    public SimpleStatisticsTracker() : base(0L, 0L)
    {
    }

    public static SimpleStatisticsTracker From(params double[] values)
        => new(values);

    public SimpleStatisticsTracker(IEnumerable<double> values) : base(values)
    {
    }

    [JsonConstructor]
    private SimpleStatisticsTracker(long n, long n0, double sum, double sse) : base(n, n0)
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
    
    protected override SimpleStatistics TakeSnapshotCore()
    {
        var variance = Count > 1L ? SumSquaredError / (Count - 1L) : 0d;
        var popVariance = SumSquaredError / Count;
        
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
        
        switch (Count - count)
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
    
    protected override SimpleStatisticsTracker CombineCore(StatisticsTracker<SimpleStatistics> other)
    {
        if (other is not SimpleStatisticsTracker sst) throw new InvalidOperationException();
        
        var n = Count + other.Count;

        if (n == 0L) return new SimpleStatisticsTracker(); 
        if (Count == 0L) return new SimpleStatisticsTracker(sst);
        if (other.Count == 0L) return new SimpleStatisticsTracker(this);
        
        var d = Sum / Count - sst.Sum / sst.Count;
        var d2 = d * d;
        var sse = SumSquaredError + sst.SumSquaredError + d2 * Count * sst.Count / n;

        return new SimpleStatisticsTracker(n, CountZero + other.CountZero, Sum + sst.Sum, sse);
    }

    public override SimpleStatisticsTracker Copy() => new (this);

    protected override SimpleStatisticsTracker MultiplyCore(double multiplier)
    {
        if (Count == 0L) return new SimpleStatisticsTracker();

        return new SimpleStatisticsTracker(Count, CountZero, Sum * multiplier, SumSquaredError * multiplier * multiplier);
    }

    protected override bool EqualsCore(StatisticsTracker<SimpleStatistics> other)
    {
        if (other is not SimpleStatisticsTracker st) return false;
        
        return Sum.Equals(st.Sum)
               && SumSquaredError.Equals(st.SumSquaredError);
    }
    
    #endregion

}