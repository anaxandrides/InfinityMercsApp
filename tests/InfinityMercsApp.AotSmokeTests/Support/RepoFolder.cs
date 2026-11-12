namespace InfinityMercsApp.AotSmokeTests.Support;

/// <summary>The repository the smoke test reads its reference files from.</summary>
public sealed record RepoFolder(string Root)
{
    private const string Marker = "InfinityMercsApp/ReferenceData/metadata.json";

    /// <summary>The folder given, else the first one above the program that holds the reference data.</summary>
    public static RepoFolder Find(string? given)
    {
        if (!string.IsNullOrEmpty(given))
        {
            return new RepoFolder(given);
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, Marker)))
            {
                return new RepoFolder(directory.FullName);
            }
        }

        throw new DirectoryNotFoundException("The repository wasn't found above the program; pass its folder as the first argument.");
    }

    public string PathOf(string relativePath) => Path.Combine(Root, relativePath);
}
