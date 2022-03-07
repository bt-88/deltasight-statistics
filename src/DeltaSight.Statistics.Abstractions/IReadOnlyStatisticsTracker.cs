using System.Diagnostics.CodeAnalysis;

namespace DeltaSight.Statistics.Abstractions;

public interface IReadOnlyStatisticsTracker<T> where T : IStatisticsSnapshot
{
    T? TakeSnapshot();
    bool TryTakeSnapshot([NotNullWhen(true)] out T? snapshot);
    bool IsEmpty();
}