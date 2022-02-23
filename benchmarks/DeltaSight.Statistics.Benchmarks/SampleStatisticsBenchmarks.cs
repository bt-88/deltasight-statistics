using BenchmarkDotNet.Attributes;

namespace DeltaSight.Statistics.Benchmarks;

public class SampleStatisticsBenchmarks
{
    private List<double> values = new (){1d, 2d, 4d};


    [Benchmark]
    public double SampleStatistics_Add()
    {
        return SampleStatistics.Empty
            .Add(values)
            .Add(Math.PI).Variance!.Value;
    }

    [Benchmark]
    public double Baseline_Add()
    {
        return values.Append(Math.PI).Variance();
    }
}

public static class EnumerableExtensions
{
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