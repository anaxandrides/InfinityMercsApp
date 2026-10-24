namespace InfinityMercsApp.Infrastructure.Repositories.InMemory;

/// <summary>Orders keys and ORDER BY columns as SQLite does: NULL first, numbers by value, text by its characters' codes (BINARY collation).</summary>
internal sealed class KeyOrder : IComparer<object>
{
    public static KeyOrder Instance { get; } = new();

    public int Compare(object? x, object? y) => (x, y) switch
    {
        (null, null) => 0,
        (null, _) => -1,
        (_, null) => 1,
        (string left, string right) => string.CompareOrdinal(left, right),
        (IComparable left, _) => left.CompareTo(y),
        _ => Comparer<object>.Default.Compare(x, y)
    };
}
