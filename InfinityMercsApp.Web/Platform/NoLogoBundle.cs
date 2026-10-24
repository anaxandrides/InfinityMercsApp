using InfinityMercsApp.Services.Logos;

namespace InfinityMercsApp.Web.Platform;

/// <inheritdoc/>
/// <remarks>The site loads each logo as it shows it (<see cref="RemoteLogoStore"/>), so it never downloads the bundle.</remarks>
public sealed class NoLogoBundle : ILogoBundleInstaller
{
    /// <inheritdoc/>
    public Task<bool> InstallAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
}
