namespace DeltaSight.Statistics.Abstractions;

public interface IReadOnlyStatisticsTracker<out T> where T : IStatisticsSnapshot
{
    T? TakeSnapshot();
    bool IsEmpty();
}