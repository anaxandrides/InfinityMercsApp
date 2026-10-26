using InfinityMercsApp.Services.Packaging;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>The app's packaged files as the site serves them under package/, read from where the repository keeps them.</summary>
internal sealed class RepoPackageFiles : IAppPackageFiles
{
    public async Task<string> ReadTextAsync(string path) => await File.ReadAllTextAsync(Located(path));

    public Task<Stream> OpenAsync(string path) => Task.FromResult<Stream>(File.OpenRead(Located(path)));

    public Task<bool> ExistsAsync(string path) => Task.FromResult(Locate(path) is not null);

    private static string Located(string path) => Locate(path) ?? throw new FileNotFoundException("The site has no such packaged file.", path);

    // The same places InfinityMercsApp.Web.csproj packages them from.
    private static string? Locate(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        var source = normalized.StartsWith("MercsIcons/", StringComparison.Ordinal)
            ? "InfinityMercsApp/Resources/Images/" + normalized
            : normalized.StartsWith("SVGCache/", StringComparison.Ordinal)
                ? "InfinityMercsApp/" + normalized
                : "InfinityMercsApp/Resources/Raw/" + normalized;
        return RepoFile.Find(source);
    }
}
