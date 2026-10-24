using InfinityMercsApp.Services.Packaging;

namespace InfinityMercsApp.Web.Platform;

/// <summary>
/// Where an image loads a logo from, for each of the view model's candidates in order: a cached logo from where the
/// logo store found it, a packaged one from package/, and a web address as it is.
/// </summary>
public sealed class LogoUrls(RemoteLogoStore logos, IAppPackageFiles packageFiles)
{
    /// <summary>The addresses to try in order; empty when there is none.</summary>
    public async Task<IReadOnlyList<string>> ForAsync(IReadOnlyList<string>? candidates)
    {
        var urls = new List<string>();
        foreach (var candidate in candidates ?? [])
        {
            if (await UrlForAsync(candidate) is { } url && !urls.Contains(url))
            {
                urls.Add(url);
            }
        }

        return urls;
    }

    private async Task<string?> UrlForAsync(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return candidate;
        }

        if (Path.IsPathRooted(candidate))
        {
            return logos.UrlFor(candidate);
        }

        return await packageFiles.ExistsAsync(candidate) ? "package/" + candidate.Replace('\\', '/').TrimStart('/') : null;
    }
}
