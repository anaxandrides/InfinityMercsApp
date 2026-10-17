namespace InfinityMercsApp.Services.Records;

/// <summary>
/// Brings the app's data (army database, companies, seasons) across when its data folder moves, as when the
/// Windows build's publisher name stopped being the template's "User Name". The old folder is copied, not moved,
/// and only into a data folder that holds nothing yet, so nothing is ever overwritten.
/// </summary>
internal static class DataFolderMove
{
    /// <returns>True when the old folder's contents were copied in.</returns>
    public static bool CopyIfNew(string oldDataDirectory, string dataDirectory)
    {
        try
        {
            if (!Directory.Exists(oldDataDirectory) ||
                Path.GetFullPath(oldDataDirectory) == Path.GetFullPath(dataDirectory) ||
                (Directory.Exists(dataDirectory) && Directory.EnumerateFileSystemEntries(dataDirectory).Any()))
            {
                return false;
            }

            foreach (var file in Directory.EnumerateFiles(oldDataDirectory, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(dataDirectory, Path.GetRelativePath(oldDataDirectory, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target) ?? dataDirectory);
                File.Copy(file, target);
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"DataFolderMove.CopyIfNew failed: {ex.Message}");
            return false;
        }
    }
}
