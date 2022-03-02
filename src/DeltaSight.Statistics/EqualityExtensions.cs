namespace DeltaSight.Statistics;

public static class EqualityExtensions
{
    public static bool ContentEquals<TKey, TValue>(this IDictionary<TKey, TValue>? a, IDictionary<TKey, TValue>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;

        foreach (var item in a)
        {
            if (!b.TryGetValue(item.Key, out var value)) return false;
            
            if (value is null && item.Value is null) continue;
            
            if (value is null || !value.Equals(item.Value)) return false;
        }

        return true;
    }
}