using DomainFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;
using DomainArmyImportResume = InfinityMercsApp.Domain.Models.Army.ArmyImportResume;
using DomainResume = InfinityMercsApp.Domain.Models.Army.Resume;
using InfinityMercsApp.Services.Logos;
using Microsoft.Maui.Storage;

namespace InfinityMercsApp.Services;

public class FactionLogoCacheService
{
    public const int DebugFactionId = 1199;
    private const int TagCompanyFactionId = 2003;
    private const int TagCompanyUnitId = 1;

    // Logical sizes each icon is actually shown at on the logo-heavy lists (see the item-template
    // WidthRequest/HeightRequest). Rasterizing these at cache-build time means the list never pays
    // the first-display raster cost. Other, less-common sizes fall back to lazy rasterization.
    private static readonly int[] FactionTileSizes = { 48 };
    private static readonly int[] UnitTileSizes = { 46, 32 };

    private const string PackagedCacheRoot = "SVGCache";
    private readonly HttpClient _httpClient;
    private readonly ISvgRasterCache _svgRasterCache;
    private readonly string _localCacheDirectory;
    private readonly string _localUnitCacheDirectory;

    public FactionLogoCacheService(HttpClient httpClient, ISvgRasterCache svgRasterCache)
    {
        _httpClient = httpClient;
        _svgRasterCache = svgRasterCache;
        _localCacheDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache");
        _localUnitCacheDirectory = Path.Combine(_localCacheDirectory, "units");
    }

    public async Task<LogoCacheResult> CacheAllAsync(IEnumerable<DomainFaction> factions, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_localCacheDirectory);
        var result = new LogoCacheResult();

        foreach (var faction in factions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalFactions++;

            if (faction.Id <= 0)
            {
                result.MissingLogoUrl++;
                continue;
            }

            var ok = await EnsureFactionLogoAvailableAsync(faction.Id, faction.Logo, cancellationToken);
            if (ok == EnsureResult.Reused)
            {
                result.CachedReuse++;
            }
            else if (ok == EnsureResult.Downloaded)
            {
                result.Downloaded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    public Task<LogoCacheResult> CacheFactionLogosFromRecordsAsync(
        IEnumerable<DomainFaction> factions,
        CancellationToken cancellationToken = default)
    {
        return CacheAllAsync(factions, cancellationToken);
    }

    public async Task<LogoCacheResult> CacheUnitLogosAsync(
        int factionId,
        IEnumerable<DomainArmyImportResume> units,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_localUnitCacheDirectory);
        var result = new LogoCacheResult();

        foreach (var unit in units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalFactions++;

            if (unit.Id <= 0)
            {
                result.MissingLogoUrl++;
                continue;
            }

            var ok = await EnsureUnitLogoAvailableAsync(factionId, unit.Id, unit.Logo, cancellationToken);
            if (ok == EnsureResult.Reused)
            {
                result.CachedReuse++;
            }
            else if (ok == EnsureResult.Downloaded)
            {
                result.Downloaded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    public async Task<LogoCacheResult> CacheUnitLogosFromRecordsAsync(
        int factionId,
        IEnumerable<DomainResume> units,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_localUnitCacheDirectory);
        var result = new LogoCacheResult();

        foreach (var unit in units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.TotalFactions++;

            if (unit.UnitId <= 0)
            {
                result.MissingLogoUrl++;
                continue;
            }

            var ok = await EnsureUnitLogoAvailableAsync(factionId, unit.UnitId, unit.Logo, cancellationToken);
            if (ok == EnsureResult.Reused)
            {
                result.CachedReuse++;
            }
            else if (ok == EnsureResult.Downloaded)
            {
                result.Downloaded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    public string GetCachedLogoPath(int factionId)
    {
        return Path.Combine(_localCacheDirectory, $"{factionId}.svg");
    }

    public string GetPackagedFactionLogoPath(int factionId)
    {
        return $"{PackagedCacheRoot}/factions/{factionId}.svg";
    }

    public string? TryGetCachedLogoPath(int factionId)
    {
        var path = GetCachedLogoPath(factionId);
        return File.Exists(path) ? path : null;
    }

    public string GetCachedUnitLogoPath(int factionId, int unitId)
    {
        return Path.Combine(_localUnitCacheDirectory, $"{factionId}-{unitId}.svg");
    }

    public string GetPackagedUnitLogoPath(int factionId, int unitId)
    {
        return $"{PackagedCacheRoot}/units/{factionId}-{unitId}.svg";
    }

    public string? TryGetCachedUnitLogoPath(int factionId, int unitId)
    {
        var path = GetCachedUnitLogoPath(factionId, unitId);
        return File.Exists(path) ? path : null;
    }

    public LogoCacheDebugInfo GetDebugInfo(int factionId, string? expectedLogoUrl = null)
    {
        var localPath = GetCachedLogoPath(factionId);
        var exists = File.Exists(localPath);
        var bytes = exists ? new FileInfo(localPath).Length : 0;

        return new LogoCacheDebugInfo
        {
            FactionId = factionId,
            ExpectedLogoUrl = expectedLogoUrl,
            LocalPath = localPath,
            Exists = exists,
            SizeBytes = bytes
        };
    }

    private async Task<EnsureResult> EnsureFactionLogoAvailableAsync(int factionId, string? logoUrl, CancellationToken cancellationToken)
    {
        var localPath = GetCachedLogoPath(factionId);
        if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
        {
            return EnsureResult.Reused;
        }

        var result = await AcquireFactionLogoAsync(factionId, logoUrl, localPath, cancellationToken);

        // The SVG is freshly on disk, so pre-render its PNGs (invalidating any stale raster first)
        // before the list ever asks for them.
        if (result == EnsureResult.Downloaded)
        {
            await WarmRastersAsync(localPath, FactionTileSizes, cancellationToken);
        }

        return result;
    }

    private async Task<EnsureResult> AcquireFactionLogoAsync(int factionId, string? logoUrl, string localPath, CancellationToken cancellationToken)
    {
        if (IsPackagedAssetPath(logoUrl))
        {
            var copied = await TryCopyPackagedAssetAsync(logoUrl!, localPath, cancellationToken);
            return copied ? EnsureResult.Downloaded : EnsureResult.NotAvailable;
        }

        if (!IsDownloadableUrl(logoUrl))
        {
            return EnsureResult.NotAvailable;
        }

        var downloaded = await TryDownloadRemoteAssetAsync(logoUrl!, localPath, cancellationToken);
        if (downloaded)
        {
            return EnsureResult.Downloaded;
        }

        // Some faction logos are missing from the CB CDN (e.g. Hayabusa / 1199 returns 404).
        // Fall back to a copy bundled with the app when one is available.
        return await TryCopyPackagedFactionFallbackAsync(factionId, localPath, cancellationToken)
            ? EnsureResult.Downloaded
            : EnsureResult.NotAvailable;
    }

    private async Task<bool> TryCopyPackagedFactionFallbackAsync(int factionId, string localPath, CancellationToken cancellationToken)
    {
        var packagedPath = GetPackagedFactionLogoPath(factionId);

        bool exists;
        try
        {
            exists = await FileSystem.Current.AppPackageFileExistsAsync(packagedPath);
        }
        catch
        {
            exists = false;
        }

        return exists && await TryCopyPackagedAssetAsync(packagedPath, localPath, cancellationToken);
    }

    private async Task<EnsureResult> EnsureUnitLogoAvailableAsync(int factionId, int unitId, string? logoUrl, CancellationToken cancellationToken)
    {
        var localPath = GetCachedUnitLogoPath(factionId, unitId);
        var forceDownload = factionId == TagCompanyFactionId && unitId == TagCompanyUnitId;
        if (!forceDownload && File.Exists(localPath) && new FileInfo(localPath).Length > 0)
        {
            return EnsureResult.Reused;
        }

        var result = await AcquireUnitLogoAsync(logoUrl, localPath, cancellationToken);

        // WarmRastersAsync invalidates first, so the forced-re-download (TagCompany) path always
        // regenerates a PNG that matches the freshly-written SVG.
        if (result == EnsureResult.Downloaded)
        {
            await WarmRastersAsync(localPath, UnitTileSizes, cancellationToken);
        }

        return result;
    }

    private async Task<EnsureResult> AcquireUnitLogoAsync(string? logoUrl, string localPath, CancellationToken cancellationToken)
    {
        if (IsPackagedAssetPath(logoUrl))
        {
            var copied = await TryCopyPackagedAssetAsync(logoUrl!, localPath, cancellationToken);
            return copied ? EnsureResult.Downloaded : EnsureResult.NotAvailable;
        }

        if (!IsDownloadableUrl(logoUrl))
        {
            return EnsureResult.NotAvailable;
        }

        var downloaded = await TryDownloadRemoteAssetAsync(logoUrl!, localPath, cancellationToken);
        return downloaded ? EnsureResult.Downloaded : EnsureResult.NotAvailable;
    }

    /// <summary>
    /// Pre-renders the standard-size PNGs for a freshly-written SVG. Invalidates any existing
    /// rasters first so a re-download never leaves a PNG of the previous artwork behind. A raster
    /// failure is swallowed — it must never fail the SVG cache result.
    /// </summary>
    private async Task WarmRastersAsync(string localPath, IReadOnlyList<int> logicalSizes, CancellationToken cancellationToken)
    {
        try
        {
            _svgRasterCache.Invalidate(localPath);
            foreach (var size in logicalSizes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _svgRasterCache.GetRasterPathAsync(localPath, size, size, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Eager raster warm failed for '{localPath}': {ex.Message}");
        }
    }

    private static bool IsPackagedAssetPath(string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && url.StartsWith(PackagedCacheRoot + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDownloadableUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static async Task<bool> TryCopyPackagedAssetAsync(string packagedPath, string localPath, CancellationToken cancellationToken)
    {
        try
        {
            var localDirectory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrWhiteSpace(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            await using var packageStream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
            await using var fileStream = File.Create(localPath);
            await packageStream.CopyToAsync(fileStream, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Packaged asset copy failed for '{packagedPath}': {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TryDownloadRemoteAssetAsync(string url, string localPath, CancellationToken cancellationToken)
    {
        try
        {
            var localDirectory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrWhiteSpace(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"SVG download failed for '{url}': HTTP {(int)response.StatusCode}.");
                return false;
            }

            await using var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = File.Create(localPath);
            await remoteStream.CopyToAsync(fileStream, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SVG download failed for '{url}': {ex.Message}");
            return false;
        }
    }

    private enum EnsureResult
    {
        Reused,
        Downloaded,
        NotAvailable
    }
}

public class LogoCacheResult
{
    public int TotalFactions { get; set; }

    public int Downloaded { get; set; }

    public int Failed { get; set; }

    public int MissingLogoUrl { get; set; }

    public int InvalidLogoUrl { get; set; }

    public int CachedReuse { get; set; }
}

public class LogoCacheDebugInfo
{
    public int FactionId { get; set; }

    public string? ExpectedLogoUrl { get; set; }

    public string LocalPath { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public long SizeBytes { get; set; }
}
