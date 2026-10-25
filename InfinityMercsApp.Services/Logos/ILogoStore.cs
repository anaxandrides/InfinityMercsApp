namespace InfinityMercsApp.Services.Logos;

/// <summary>
/// Where the logo cache keeps logos: files on the device in the app, or on the web site only where each is found, for
/// the browser to load itself.
/// </summary>
public interface ILogoStore
{
    /// <summary>Whether a logo is kept at <paramref name="localPath"/>.</summary>
    bool Has(string localPath);

    /// <summary>Keeps the logo at <paramref name="url"/> as <paramref name="localPath"/>; false when it couldn't be had.</summary>
    Task<bool> FetchAsync(string url, string localPath, CancellationToken cancellationToken);

    /// <summary>Keeps a logo the app bundles as <paramref name="localPath"/>; false when it couldn't be had.</summary>
    Task<bool> CopyPackagedAsync(string packagedPath, string localPath, CancellationToken cancellationToken);
}
