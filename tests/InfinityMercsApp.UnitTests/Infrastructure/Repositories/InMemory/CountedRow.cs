using SQLite;

namespace InfinityMercsApp.UnitTests.Infrastructure.Repositories.InMemory;

/// <summary>A row keyed by an auto-incrementing integer, like most of the army tables.</summary>
public sealed class CountedRow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Column("display_name")]
    public string? Name { get; set; }

    public int Rank { get; set; }
}
