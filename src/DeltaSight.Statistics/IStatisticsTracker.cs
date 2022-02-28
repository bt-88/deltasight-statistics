namespace DeltaSight.Statistics;

public interface IStatisticsTracker<T>
{
    void Add(double value, long count = 1L);
    void Add(params double[] values);
    void Add(IEnumerable<double> values);
    void Remove(double value, long count = 1L);
    void Remove(params double[] values);
    void Remove(IEnumerable<double> values);
    void Clear();
    T? TakeSnapshot();
    bool IsEmpty();
    IStatisticsTracker<T> Multiply(double multiplier);
    IStatisticsTracker<T> Add(IStatisticsTracker<T> other);
}