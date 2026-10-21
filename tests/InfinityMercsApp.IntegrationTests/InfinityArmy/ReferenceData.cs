namespace InfinityMercsApp.IntegrationTests.InfinityArmy;

/// <summary>The API responses checked into the app, which its JSON reading was written against.</summary>
internal static class ReferenceData
{
    public static string Army101 => Read("army-101.json");

    public static string Metadata => Read("metadata.json");

    private static string Read(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "InfinityMercsApp", "ReferenceData", fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        throw new FileNotFoundException($"InfinityMercsApp/ReferenceData/{fileName} was not found above {AppContext.BaseDirectory}.");
    }
}
