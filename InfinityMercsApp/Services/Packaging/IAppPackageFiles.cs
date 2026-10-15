namespace InfinityMercsApp.Services.Packaging;

/// <summary>Reads files bundled with the app, injectable so tests can supply their own.</summary>
public interface IAppPackageFiles
{
    Task<string> ReadTextAsync(string path);

    Task<bool> ExistsAsync(string path);
}
