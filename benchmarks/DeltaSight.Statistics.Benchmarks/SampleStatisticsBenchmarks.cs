using BenchmarkDotNet.Attributes;

namespace DeltaSight.Statistics.Benchmarks;

public class SampleStatisticsBenchmarks
{
    private List<double> _values = new (){1d, 1d, 2d, 3d, 2d, 4d, Math.PI};
    private SampleStatistics _stats;

    public SampleStatisticsBenchmarks()
    {
        _stats = _values.CreateStatistics();
    }

    [Benchmark]
    public double? SampleStatistics_Compute()
    {
        return _values.CreateStatistics().Variance;
    }

    [Benchmark]
    public double? SampleStatistics_Add()
    {
        return _stats.Add(Math.PI).Variance;
    }
    
    [Benchmark]
    public double? SampleStatistics_Remove()
    {
        return _stats.Remove(Math.PI).Variance;
    }
    
    [Benchmark]
    public double OnePass_Compute()
    {
        return _values.Variance();
    }

    [Benchmark]
    public double OnePass_Add()
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