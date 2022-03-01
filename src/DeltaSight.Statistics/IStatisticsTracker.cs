namespace DeltaSight.Statistics;


public interface IStatisticsTracker : IStatisticsTracker<IStatisticsSnapshot>
{
    
}

public interface IStatisticsTracker<out T> : IReadOnlyStatisticsTracker<T> where T : IStatisticsSnapshot
{
    void Add(double value, long count = 1L);
    void Add(params double[] values);
    void Add(IEnumerable<double> values);
    void Remove(double value, long count = 1L);
    void Remove(params double[] values);
    void Remove(IEnumerable<double> values);
    void Clear();
}

public interface IReadOnlyStatisticsTracker<out T> where T : IStatisticsSnapshot
{
    T? TakeSnapshot();
    bool IsEmpty();
}

public interface IStatisticsSnapshot
{
}