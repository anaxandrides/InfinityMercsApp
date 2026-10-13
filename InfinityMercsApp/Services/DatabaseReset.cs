using InfinityMercsApp.Infrastructure.Repositories;

namespace InfinityMercsApp.Services;

/// <summary>
/// Deletes a working database whose layout is older than the current one (see <see cref="DatabaseSchema"/>),
/// so the app starts from scratch and the network import rebuilds it.
/// </summary>
internal static class DatabaseReset
{
    public static void DiscardIfOutdated(string databasePath)
    {
        try
        {
            if (!File.Exists(databasePath) || DatabaseSchema.ReadVersion(databasePath) >= DatabaseSchema.CurrentVersion)
            {
                return;
            }

            foreach (var suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
            {
                if (File.Exists(databasePath + suffix))
                {
                    File.Delete(databasePath + suffix);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DatabaseReset.DiscardIfOutdated failed: {ex.Message}");
        }
    }
}
