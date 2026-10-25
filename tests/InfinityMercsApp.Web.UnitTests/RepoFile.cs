namespace InfinityMercsApp.Web.UnitTests;

/// <summary>Reads a file checked into the repository, such as the reference army data.</summary>
internal static class RepoFile
{
    public static string Read(string relativePath)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        throw new FileNotFoundException("Not found above the test's folder.", relativePath);
    }
}
