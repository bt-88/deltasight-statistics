namespace DeltaSight.Statistics.Abstractions;


public interface IStatisticsTracker<out T> : IReadOnlyStatisticsTracker<T> where T : IStatisticsSnapshot
{
    void Add(double value, long count = 1L);
    void Add(IEnumerable<KeyValuePair<double, int>>? hist);
    void Add(IEnumerable<KeyValuePair<double, long>>? hist);
    void Add(IEnumerable<double> values);
    void Remove(double value, long count = 1L);
    void Remove(IEnumerable<double> values);
    void Clear();
}