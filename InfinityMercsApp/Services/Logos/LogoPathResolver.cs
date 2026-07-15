using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using Microsoft.Maui.Storage;

namespace InfinityMercsApp.Services.Logos;

/// <summary>
/// Shared resolution of a viewer item's logo SVG. Cached (downloaded) path first, then the
/// packaged fallback, using the exact same candidate ordering and per-item-type id math that
/// previously lived privately in <c>FactionListItemView</c>. Both the raster cache and any
/// display control resolve icons identically through this helper.
/// </summary>
public static class LogoPathResolver
{
    /// <summary>
    /// Candidate on-disk (downloaded) SVG paths for the item, in priority order.
    /// </summary>
    public static IEnumerable<string?> BuildCachedCandidates(IViewerListItem item)
    {
        yield return item.CachedLogoPath;

        if (item is ICompanyMercsEntry mercEntry &&
            mercEntry.LogoSourceFactionId > 0 &&
            mercEntry.LogoSourceUnitId > 0)
        {
            yield return Path.Combine(
                FileSystem.Current.AppDataDirectory,
                "svg-cache",
                "units",
                $"{mercEntry.LogoSourceFactionId}-{mercEntry.LogoSourceUnitId}.svg");
        }

        if (item is ArmyUnitSelectionItem unit)
        {
            yield return Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache", "units", $"{unit.SourceFactionId}-{unit.Id}.svg");
        }
        else if (item is ArmyFactionSelectionItem faction)
        {
            yield return Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache", $"{faction.Id}.svg");
        }
    }

    /// <summary>
    /// Candidate packaged (app-bundled) SVG logical names for the item, in priority order.
    /// </summary>
    public static IEnumerable<string?> BuildPackagedCandidates(IViewerListItem item)
    {
        yield return item.PackagedLogoPath;

        if (item is ICompanyMercsEntry mercEntry &&
            mercEntry.LogoSourceFactionId > 0 &&
            mercEntry.LogoSourceUnitId > 0)
        {
            yield return $"SVGCache/units/{mercEntry.LogoSourceFactionId}-{mercEntry.LogoSourceUnitId}.svg";
        }

        if (item is ArmyUnitSelectionItem unit)
        {
            yield return $"SVGCache/units/{unit.SourceFactionId}-{unit.Id}.svg";
        }
        else if (item is ArmyFactionSelectionItem faction)
        {
            yield return $"SVGCache/factions/{faction.Id}.svg";
        }
    }

    /// <summary>
    /// Returns the first cached SVG path that exists on disk, otherwise the first packaged
    /// logical name that is bundled with the app, otherwise <c>null</c>. Returns the path/logical
    /// name (not a stream); the caller opens it (<c>File.OpenRead</c> for a real path,
    /// <c>OpenAppPackageFileAsync</c> for a packaged logical name).
    /// </summary>
    public static async Task<string?> ResolveBestSvgPathAsync(IViewerListItem item)
    {
        foreach (var cachedPath in BuildCachedCandidates(item))
        {
            if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
            {
                return cachedPath;
            }
        }

        foreach (var packagedPath in BuildPackagedCandidates(item))
        {
            if (string.IsNullOrWhiteSpace(packagedPath))
            {
                continue;
            }

            bool exists;
            try
            {
                exists = await FileSystem.Current.AppPackageFileExistsAsync(packagedPath);
            }
            catch
            {
                exists = false;
            }

            if (exists)
            {
                return packagedPath;
            }
        }

        return null;
    }
}
