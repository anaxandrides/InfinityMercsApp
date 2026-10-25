using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;
using Microsoft.Data.Sqlite;

namespace InfinityMercsApp.UnitTests.Infrastructure.Repositories;

/// <summary>The reference army data imported once into SQLite and once into memory, shared by the comparison tests.</summary>
public sealed class ArmyDataInBothRepositories : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"in-memory-compare-{Guid.NewGuid():N}.db3");

    public ArmyDataInBothRepositories()
    {
        Sqlite = ImportedArmyData.IntoAsync(new SQLiteRepository(new SQLIteConfiguration { DBPath = _databasePath }));
        Memory = ImportedArmyData.IntoAsync(new InMemoryRepository());
    }

    internal Task<ImportedArmyData> Sqlite { get; }

    internal Task<ImportedArmyData> Memory { get; }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in (IReadOnlyList<string>)[string.Empty, "-wal", "-shm"])
        {
            try
            {
                File.Delete(_databasePath + suffix);
            }
            catch (IOException)
            {
                // The repository keeps its connection open; a leftover temp file is harmless.
            }
        }
    }
}
