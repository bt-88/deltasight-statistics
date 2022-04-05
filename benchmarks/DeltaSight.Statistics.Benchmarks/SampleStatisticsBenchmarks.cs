using BenchmarkDotNet.Attributes;
using MathNet.Numerics.Statistics;

namespace DeltaSight.Statistics.Benchmarks;

public class SampleStatisticsBenchmarks
{
    private readonly List<double> _values = new (){1d, 1d, 2d, 3d, 2d, 4d, Math.PI};

    [Benchmark]
    [BenchmarkCategory("Compute")]
    public double Compute_Variance_TrackSimpleStatistics()
    {
        return _values.TrackSimpleStatistics().TakeSnapshot().Variance;
    }

    [Benchmark]
    [BenchmarkCategory("Compute")]
    public double Compute_Variance_RunningStatistics()
    {
        return new RunningStatistics(_values).Variance;
    }

    [Benchmark]
    [BenchmarkCategory("Compute")]
    public double Compute_Variance_ExtensionMethod()
    {
        return _values.Variance();
    }

    [Benchmark]
    [BenchmarkCategory("AddFiveTimes")]
    public double AddFiveTimes_SimpleStatisticsTracker()
    {
        var tracker = new SimpleStatisticsTracker(_values);
        tracker.Add(Math.PI, 5L);
        return tracker.TakeSnapshot()!.Variance;
    }

    [Benchmark]
    [BenchmarkCategory("AddFiveTimes")]
    public double AddFiveTimes_RunningStatistics()
    {
        var runningStats = new RunningStatistics(_values);
        
        runningStats.Push(Math.PI);
        runningStats.Push(Math.PI);
        runningStats.Push(Math.PI);
        runningStats.Push(Math.PI);
        runningStats.Push(Math.PI);

        return runningStats.Variance;
    }
    
    [Benchmark]
    [BenchmarkCategory("AddFiveTimes")]
    public double AddFiveTimes_ExtensionMethod()
    {
        return _values
            .Append(Math.PI)
            .Append(Math.PI)
            .Append(Math.PI)
            .Append(Math.PI)
            .Append(Math.PI)
            .Variance();
    }

    [Benchmark]
    [BenchmarkCategory("Add")]
    public double Add_SimpleStatisticsTracker()
    {
        var tracker = new SimpleStatisticsTracker(_values);
        
        tracker.Add(Math.PI);

        return tracker.TakeSnapshot()!.Variance;
    }
    
    [Benchmark]
    [BenchmarkCategory("Add")]
    public double Add_RunningStatistics()
    {
        var runningStats = new RunningStatistics(_values);
        
        runningStats.Push(Math.PI);

        return runningStats.Variance;
    }

    [Benchmark]
    [BenchmarkCategory("Add")]
    public double Add_ExtensionMethod()
    {
        return _values.Append(Math.PI).Variance();
    }
}

public static class EnumerableExtensions
{
    /// <summary>
    /// One pass algorithm for variance
    /// </summary>
    public static double Variance(this IEnumerable<double> source) 
    { 
        var n = 0L;
        var mean = 0d;
        var m2 = 0d;

        foreach (var x in source)
        {
            n = n + 1;
            var delta = x - mean;
            mean = mean + delta / n;
            m2 += delta * (x - mean);
        }
        
        return m2 / (n - 1);
    }
}