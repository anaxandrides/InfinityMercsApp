using InfinityMercsApp.Services.Packaging;

namespace InfinityMercsApp.AotSmokeTests.Support;

/// <summary>The app's packaged files (markets, glossary), read from where the repository keeps them.</summary>
public sealed class RepoPackageFiles(RepoFolder repo) : IAppPackageFiles
{
    public Task<string> ReadTextAsync(string path) => File.ReadAllTextAsync(PathOf(path));

    public Task<Stream> OpenAsync(string path) => Task.FromResult<Stream>(File.OpenRead(PathOf(path)));

    public Task<bool> ExistsAsync(string path) => Task.FromResult(File.Exists(PathOf(path)));

    private string PathOf(string path) => repo.PathOf(Path.Combine("InfinityMercsApp/Resources/Raw", path.Replace('\\', '/').TrimStart('/')));
}
