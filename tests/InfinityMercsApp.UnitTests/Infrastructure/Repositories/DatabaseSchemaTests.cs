using InfinityMercsApp.Infrastructure.Repositories;
using SQLite;

namespace InfinityMercsApp.UnitTests.Infrastructure.Repositories;

public sealed class DatabaseSchemaTests : IDisposable
{
    private readonly SQLiteConnection _connection = new(":memory:");

    public void Dispose() => _connection.Close();

    [Fact]
    public void StampIfNew_EmptyDatabase_StampsTheCurrentVersion()
    {
        DatabaseSchema.StampIfNew(_connection);

        Assert.Equal(DatabaseSchema.CurrentVersion, DatabaseSchema.ReadVersion(_connection));
    }

    [Fact]
    public void StampIfNew_ArmiesWithoutAVersion_StaysUnversioned()
    {
        _connection.Execute("CREATE TABLE armies (id INTEGER)");

        DatabaseSchema.StampIfNew(_connection);

        Assert.Equal(0, DatabaseSchema.ReadVersion(_connection));
    }

    [Fact]
    public void StampIfNew_AlreadyVersioned_KeepsTheVersion()
    {
        DatabaseSchema.WriteVersion(_connection, 1);

        DatabaseSchema.StampIfNew(_connection);

        Assert.Equal(1, DatabaseSchema.ReadVersion(_connection));
    }
}
