using SQLite;

namespace InfinityMercsApp.UnitTests.Infrastructure.Repositories.InMemory;

/// <summary>A row keyed by text, which SQLite keeps in the order rows were added.</summary>
public sealed class CodedRow
{
    [PrimaryKey]
    public string Code { get; set; } = string.Empty;

    public int Value { get; set; }
}
