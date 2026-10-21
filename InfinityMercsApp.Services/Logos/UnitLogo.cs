namespace InfinityMercsApp.Services.Logos;

/// <summary>Chooses which logo file to show for a unit or faction.</summary>
public static class UnitLogo
{
    /// <summary>The downloaded logo when it exists on disk, otherwise the packaged one.</summary>
    public static string? Source(string? cachedPath, string? packagedPath) =>
        !string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath) ? cachedPath : packagedPath;

    /// <summary>
    /// The logos to try in order, for a view that shows the first one it can load: the downloaded file,
    /// the packaged asset (as a relative asset path), then a fallback such as the TAG icon. Blanks are left out.
    /// </summary>
    public static IReadOnlyList<string> Candidates(string? cachedPath, string? packagedPath, string? fallbackPath = null)
    {
        List<string?> paths = [cachedPath, PackagedAssetPath(packagedPath), fallbackPath];
        return paths
            .OfType<string>()
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    // Packaged assets are relative with forward slashes: "\SVGCache\x.svg" is "SVGCache/x.svg".
    private static string? PackagedAssetPath(string? path) => path?.Replace('\\', '/').TrimStart('/');
}
