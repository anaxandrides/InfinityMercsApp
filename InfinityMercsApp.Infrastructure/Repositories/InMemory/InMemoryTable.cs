using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SQLite;

namespace InfinityMercsApp.Infrastructure.Repositories.InMemory;

/// <summary>
/// One table's rows, kept as copies so a caller changing a row it read changes nothing until it updates it. Rows come
/// back in the order SQLite scans them: by key when the key is an integer (it is the rowid), otherwise in the order
/// they were added.
/// </summary>
internal sealed class InMemoryTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
    where T : new()
{
    private static readonly MethodInfo MemberwiseCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(nameof(Object), "MemberwiseClone");

    private static readonly PropertyInfo? Key = typeof(T).GetProperties()
        .FirstOrDefault(property => property.GetCustomAttribute<PrimaryKeyAttribute>() is not null);

    private static readonly bool AutoIncrements = Key?.GetCustomAttribute<AutoIncrementAttribute>() is not null;

    private static readonly bool KeyIsRowId = Key?.PropertyType == typeof(int) || Key?.PropertyType == typeof(long);

    private readonly List<T> _rows = [];
    private readonly Dictionary<object, int> _indexByKey = [];
    private int _lastId;
    private bool _sorted = true;

    public IEnumerable<T> Rows
    {
        get
        {
            if (KeyIsRowId && !_sorted)
            {
                _rows.Sort((left, right) => KeyOrder.Instance.Compare(KeyOf(left), KeyOf(right)));
                Reindex();
                _sorted = true;
            }

            return _rows;
        }
    }

    public static T Copy(T row) => (T)(MemberwiseCloneMethod.Invoke(row, null) ?? throw new InvalidOperationException("A row couldn't be copied."));

    public void Insert(T row)
    {
        if (Key is not null && AutoIncrements && Key.GetValue(row) is 0)
        {
            Key.SetValue(row, ++_lastId);
        }
        else if (Key is not null && Key.GetValue(row) is int id)
        {
            _lastId = Math.Max(_lastId, id);
        }

        var copy = Copy(row);
        if (KeyOf(copy) is { } key)
        {
            if (!_indexByKey.TryAdd(key, _rows.Count))
            {
                throw new InvalidOperationException($"{typeof(T).Name} already has a row with key {key}.");
            }

            _sorted = _sorted && (_rows.Count == 0 || KeyOrder.Instance.Compare(KeyOf(_rows[^1]), key) < 0);
        }

        _rows.Add(copy);
    }

    public void Update(T row)
    {
        if (Key is null)
        {
            throw new InvalidOperationException($"{typeof(T).Name} has no primary key, so its rows can't be updated.");
        }

        if (KeyOf(row) is { } key && _indexByKey.TryGetValue(key, out var index))
        {
            _rows[index] = Copy(row);
        }
    }

    public T? Find(object key) => _indexByKey.TryGetValue(key, out var index) ? _rows[index] : default;

    public void RemoveWhere(Func<T, bool> filter)
    {
        if (_rows.RemoveAll(row => filter(row)) > 0)
        {
            Reindex();
        }
    }

    private void Reindex()
    {
        _indexByKey.Clear();
        for (var index = 0; index < _rows.Count; index++)
        {
            if (KeyOf(_rows[index]) is { } key)
            {
                _indexByKey[key] = index;
            }
        }
    }

    private static object? KeyOf(T row) => Key?.GetValue(row);

    /// <summary>The property a column is read from: the one named so, or mapped to it by [Column].</summary>
    public static PropertyInfo ColumnProperty(string columnName) =>
        typeof(T).GetProperties().FirstOrDefault(property =>
            string.Equals(property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name, columnName, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"{typeof(T).Name} has no column {columnName}.", nameof(columnName));
}
