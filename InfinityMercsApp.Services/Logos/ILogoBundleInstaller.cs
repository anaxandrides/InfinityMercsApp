namespace InfinityMercsApp.Services.Logos;

/// <summary>Fills the logo cache from the API's single zip of every logo, so a first launch makes one download.</summary>
public interface ILogoBundleInstaller
{
    /// <summary>True when the current bundle is in place (installed now or before); false when it couldn't be had.</summary>
    Task<bool> InstallAsync(CancellationToken cancellationToken = default);
}
