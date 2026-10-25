using System.Globalization;
using System.Linq.Expressions;

namespace InfinityMercsApp.Infrastructure.Repositories.InMemory;

/// <summary>
/// The repository kept in memory, for the web site, which can't open SQLite in the browser: the same import fills it
/// and the same providers read it, with the same results as SQLite gives them (see <see cref="SqliteTextSemantics"/>).
/// </summary>
public sealed class InMemoryRepository : ISQLiteRepository
{
    private readonly Dictionary<Type, object> _tables = [];
    private readonly Lock _sync = new();

    /// <inheritdoc/>
    public void Insert<T>(IEnumerable<T> recordsToInsert) where T : new()
    {
        lock (_sync)
        {
            var table = Table<T>();
            foreach (var record in recordsToInsert)
            {
                table.Insert(record);
            }
        }
    }

    /// <inheritdoc/>
    public void Update<T>(T item) where T : new()
    {
        lock (_sync)
        {
            Table<T>().Update(item);
        }
    }

    /// <inheritdoc/>
    public T? GetById<T>(int id) where T : new()
    {
        lock (_sync)
        {
            return Table<T>().Find(id) is { } row ? InMemoryTable<T>.Copy(row) : default;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy = null) where T : new() =>
        Query(filter, orderBy, int.MaxValue);

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy, int limit) where T : new() =>
        Query(filter, orderBy, limit);

    /// <inheritdoc/>
    public T? FirstOrDefault<T>(Expression<Func<T, bool>> filter) where T : new() =>
        Query(filter, null, 1) is [var first] ? first : default;

    /// <inheritdoc/>
    public int Count<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        var matches = Compile(filter);
        lock (_sync)
        {
            return Table<T>().Rows.Count(matches);
        }
    }

    /// <inheritdoc/>
    public bool Exists<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        var matches = Compile(filter);
        lock (_sync)
        {
            return Table<T>().Rows.Any(matches);
        }
    }

    /// <inheritdoc/>
    public bool Any<T>() where T : new()
    {
        lock (_sync)
        {
            return Table<T>().Rows.Any();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<TResult> QueryColumn<T, TResult>(string columnName) where T : new()
    {
        var property = InMemoryTable<T>.ColumnProperty(columnName);
        lock (_sync)
        {
            return Table<T>().Rows.Select(row => ValueOf<TResult>(property.GetValue(row), columnName)).ToList();
        }
    }

    /// <inheritdoc/>
    public void Delete<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        var matches = Compile(filter);
        lock (_sync)
        {
            Table<T>().RemoveWhere(matches);
        }
    }

    /// <inheritdoc/>
    public void DeleteAll<T>() where T : new()
    {
        lock (_sync)
        {
            Table<T>().RemoveWhere(_ => true);
        }
    }

    /// <inheritdoc/>
    /// <remarks>Nothing is rolled back if <paramref name="action"/> throws; the site's data is only ever imported whole.</remarks>
    public void RunInTransaction(Action action)
    {
        lock (_sync)
        {
            action();
        }
    }

    private List<T> Query<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy, int limit) where T : new()
    {
        var matches = Compile(filter);
        lock (_sync)
        {
            var rows = Table<T>().Rows.Where(matches);
            if (orderBy is not null)
            {
                rows = rows.OrderBy(orderBy.Compile(), KeyOrder.Instance);
            }

            return rows.Take(limit).Select(InMemoryTable<T>.Copy).ToList();
        }
    }

    private static TResult ValueOf<TResult>(object? value, string columnName) => value switch
    {
        TResult typed => typed,
        null => throw new InvalidOperationException($"Column {columnName} holds NULL."),
        _ => (TResult)Convert.ChangeType(value, typeof(TResult), CultureInfo.InvariantCulture)
    };

    private static Func<T, bool> Compile<T>(Expression<Func<T, bool>> filter) =>
        ((Expression<Func<T, bool>>)SqliteTextSemantics.Instance.Visit(filter)).Compile();

    private InMemoryTable<T> Table<T>() where T : new()
    {
        if (!_tables.TryGetValue(typeof(T), out var table))
        {
            table = new InMemoryTable<T>();
            _tables[typeof(T)] = table;
        }

        return (InMemoryTable<T>)table;
    }
}
