namespace InfinityMercsApp.UnitTests.Characterization;

internal static class RepoFiles
{
    public static string Resolve(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}' above '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Resolves the repository root (the directory holding InfinityMercsApp.sln).
    /// </summary>
    public static string RepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "InfinityMercsApp.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate the repository root above '{AppContext.BaseDirectory}'.");
    }
}
