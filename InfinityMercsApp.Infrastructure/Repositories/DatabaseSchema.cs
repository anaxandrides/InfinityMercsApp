namespace InfinityMercsApp.Infrastructure.Repositories;

using SQLite;

/// <summary>
/// The version of the database layout, kept in SQLite's <c>PRAGMA user_version</c>.
/// </summary>
/// <remarks>
/// Version history:
/// <list type="bullet">
/// <item><description>0/1: army data stored as JSON text columns (<c>ProfileGroupsJson</c>, <c>FiltersJson</c>...).</description></item>
/// <item><description>2: army data stored in typed tables (<c>unit_profiles</c>, <c>army_filter_items</c>...).</description></item>
/// </list>
/// A database older than <see cref="CurrentVersion"/> is replaced by the bundled seed at startup.
/// </remarks>
public static class DatabaseSchema
{
    public const int CurrentVersion = 2;

    private const string ArmiesTable = "armies";

    public static int ReadVersion(SQLiteConnection connection) =>
        connection.ExecuteScalar<int>("PRAGMA user_version");

    public static void WriteVersion(SQLiteConnection connection, int version) =>
        connection.Execute($"PRAGMA user_version = {version}");

    /// <summary>
    /// Stamps a database that has no army data yet with <see cref="CurrentVersion"/>, so a database built up by
    /// the network import is recognised as current. Databases that already hold army data keep their version.
    /// </summary>
    public static void StampIfNew(SQLiteConnection connection)
    {
        if (ReadVersion(connection) != 0)
        {
            return;
        }

        var hasArmies = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", ArmiesTable) > 0;
        if (!hasArmies)
        {
            WriteVersion(connection, CurrentVersion);
        }
    }

    /// <summary>The schema version of the database file at <paramref name="path"/>.</summary>
    public static int ReadVersion(string path)
    {
        var connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadOnly);
        try
        {
            return ReadVersion(connection);
        }
        finally
        {
            connection.Close();
        }
    }
}
