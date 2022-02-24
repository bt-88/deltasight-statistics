using BenchmarkDotNet.Attributes;
using MathNet.Numerics.Statistics;

namespace DeltaSight.Statistics.Benchmarks;

public class SampleStatisticsBenchmarks
{
    private readonly List<double> _values = new (){1d, 1d, 2d, 3d, 2d, 4d, Math.PI};
    private readonly SampleStatistics _stats;
    private readonly RunningStatistics _runningStats;

    public SampleStatisticsBenchmarks()
    {
        _stats = _values.CreateStatistics();
        _runningStats = new RunningStatistics(_values);
    }

    [Benchmark]
    public double? SampleStatistics_Compute()
    {
        return _values.CreateStatistics().Variance;
    }

    [Benchmark]
    public double MathNet_Compute()
    {
        return new RunningStatistics(_values).Variance;
    }

    [Benchmark]
    public double Baseline_Compute()
    {
        return _values.Variance();
    }

    [Benchmark]
    public double? SampleStatistics_Add()
    {
        return _stats.Add(Math.PI).Variance;
    }
    
    [Benchmark]
    public double? SampleStatistics_AddFiveTimes()
    {
        return _stats.Add(Math.PI, 5L).Variance;
    }

    [Benchmark]
    public double MathNet_AddFiveTimes()
    {
        _runningStats.Push(Math.PI);
        _runningStats.Push(Math.PI);
        _runningStats.Push(Math.PI);
        _runningStats.Push(Math.PI);
        _runningStats.Push(Math.PI);

        return _runningStats.Variance;
    }
    
    [Benchmark]
    public double Baseline_AddFiveTimes()
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
    public double MathNet_Add()
    {
        _runningStats.Push(Math.PI);

        return _runningStats.Variance;
    }

    [Benchmark]
    public double Baseline_Add()
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