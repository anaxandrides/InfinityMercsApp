using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.Services.Import;
using SQLite;

namespace InfinityMercsApp.UnitTests.Services.Import;

/// <summary>A database laid out by an older version of the app is thrown away, so the import rebuilds it.</summary>
public sealed class DatabaseResetTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("database-reset-").FullName;

    private string DatabasePath => Path.Combine(_folder, "mercs.db3");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void DiscardIfOutdated_OlderLayout_DeletesItAndItsSideFiles()
    {
        Create(version: 1);
        File.WriteAllText(DatabasePath + "-wal", string.Empty);
        File.WriteAllText(DatabasePath + "-journal", string.Empty);

        DatabaseReset.DiscardIfOutdated(DatabasePath);

        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public void DiscardIfOutdated_CurrentLayout_KeepsIt()
    {
        Create(DatabaseSchema.CurrentVersion);

        DatabaseReset.DiscardIfOutdated(DatabasePath);

        Assert.True(File.Exists(DatabasePath));
    }

    [Fact]
    public void DiscardIfOutdated_NoDatabase_DoesNothing()
    {
        DatabaseReset.DiscardIfOutdated(DatabasePath);

        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public void DiscardIfOutdated_NotADatabase_LeavesIt()
    {
        File.WriteAllText(DatabasePath, "not sqlite at all, but long enough to have a header of sorts......................");

        DatabaseReset.DiscardIfOutdated(DatabasePath);

        Assert.True(File.Exists(DatabasePath));
    }

    private void Create(int version)
    {
        var connection = new SQLiteConnection(DatabasePath);
        try
        {
            DatabaseSchema.WriteVersion(connection, version);
        }
        finally
        {
            connection.Close();
        }
    }
}
