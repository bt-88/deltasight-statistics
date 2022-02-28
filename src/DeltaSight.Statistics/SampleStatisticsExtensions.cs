namespace DeltaSight.Statistics;

public static class SampleStatisticsExtensions
{
    /// <summary>
    /// Creates a statistics tracker from an <see cref="IEnumerable{T}"/> <paramref name="source"/>
    /// </summary>
    /// <param name="source">Value source</param>
    /// <returns>A new <see cref="SimpleStatisticsTracker"/></returns>
    public static SimpleStatisticsTracker TrackSimpleStatistics(this IEnumerable<double> source)
    {
        return new SimpleStatisticsTracker(source);
    }
}