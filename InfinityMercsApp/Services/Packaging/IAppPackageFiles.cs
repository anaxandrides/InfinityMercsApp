namespace InfinityMercsApp.Services.Packaging;

/// <summary>Reads files bundled with the app, injectable so tests can supply their own.</summary>
public interface IAppPackageFiles
{
    Task<string> ReadTextAsync(string path);

    /// <summary>Opens a bundled file for reading; the caller disposes the stream.</summary>
    Task<Stream> OpenAsync(string path);

    Task<bool> ExistsAsync(string path);
}
