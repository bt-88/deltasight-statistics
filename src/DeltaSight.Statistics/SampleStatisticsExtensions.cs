namespace DeltaSight.Statistics;

public static class SampleStatisticsExtensions
{
    public static SampleStatistics CreateStatistics(this IEnumerable<double> source)
    {
        return SampleStatistics.Empty.Add(source);
    }
}