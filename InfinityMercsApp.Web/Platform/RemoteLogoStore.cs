using System.Collections.Concurrent;
using InfinityMercsApp.Services.Logos;

namespace InfinityMercsApp.Web.Platform;

/// <inheritdoc/>
/// <remarks>
/// The site keeps no logo files: thousands of them would fill the browser's memory. It notes where each logo is found,
/// and the page's images load it from there; one that fails to load falls back to the next candidate.
/// </remarks>
public sealed class RemoteLogoStore : ILogoStore
{
    private readonly ConcurrentDictionary<string, string> _urls = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Where the logo cached as <paramref name="localPath"/> is found; null when it isn't known.</summary>
    public string? UrlFor(string localPath) => _urls.TryGetValue(localPath, out var url) ? url : null;

    /// <inheritdoc/>
    public bool Has(string localPath) => _urls.ContainsKey(localPath);

    /// <inheritdoc/>
    public Task<bool> FetchAsync(string url, string localPath, CancellationToken cancellationToken)
    {
        _urls[localPath] = url;
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> CopyPackagedAsync(string packagedPath, string localPath, CancellationToken cancellationToken)
    {
        _urls[localPath] = "package/" + packagedPath.Replace('\\', '/').TrimStart('/');
        return Task.FromResult(true);
    }
}
