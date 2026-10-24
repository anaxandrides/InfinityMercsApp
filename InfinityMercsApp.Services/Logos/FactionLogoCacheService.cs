using System.Diagnostics.CodeAnalysis;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using DomainArmyImportResume = InfinityMercsApp.Domain.Models.Army.ArmyImportResume;
using DomainFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;
using DomainResume = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Services.Logos;

public class FactionLogoCacheService
{
    private const int TagCompanyFactionId = 2003;
    private const int TagCompanyUnitId = 1;

    // Enough to hide the network's latency without crowding the CDN.
    private const int MaxConcurrentDownloads = 8;

    private const string PackagedCacheRoot = "SVGCache";
    private readonly ILogoStore _store;
    private readonly IAppPackageFiles _packageFiles;
    private readonly string _localCacheDirectory;
    private readonly string _localUnitCacheDirectory;

    public FactionLogoCacheService(ILogoStore store, RecordPaths paths, IAppPackageFiles packageFiles)
    {
        _store = store;
        _packageFiles = packageFiles;
        _localCacheDirectory = paths.FactionLogoDirectory;
        _localUnitCacheDirectory = Path.Combine(_localCacheDirectory, "units");
    }

    /// <summary>Has every faction's logo on disk, downloading those missing (several at once).</summary>
    public Task<LogoCacheResult> CacheAllAsync(IEnumerable<DomainFaction> factions, CancellationToken cancellationToken = default)
    {
        return CacheAsync(
            factions.Select(faction => faction.Id > 0 ? FactionLogo(faction.Id, faction.Logo) : null).ToList(),
            cancellationToken);
    }

    public Task<LogoCacheResult> CacheFactionLogosFromRecordsAsync(
        IEnumerable<DomainFaction> factions,
        CancellationToken cancellationToken = default)
    {
        return CacheAllAsync(factions, cancellationToken);
    }

    /// <summary>Has every unit's logo in an army on disk, downloading those missing (several at once).</summary>
    public Task<LogoCacheResult> CacheUnitLogosAsync(
        int factionId,
        IEnumerable<DomainArmyImportResume> units,
        CancellationToken cancellationToken = default)
    {
        return CacheAsync(
            units.Select(unit => unit.Id > 0 ? UnitLogo(factionId, unit.Id, unit.Logo) : null).ToList(),
            cancellationToken);
    }

    public Task<LogoCacheResult> CacheUnitLogosFromRecordsAsync(
        int factionId,
        IEnumerable<DomainResume> units,
        CancellationToken cancellationToken = default)
    {
        return CacheAsync(
            units.Select(unit => unit.UnitId > 0 ? UnitLogo(factionId, unit.UnitId, unit.Logo) : null).ToList(),
            cancellationToken);
    }

    /// <summary>Whether the cache holds a logo at <paramref name="localPath"/> (on the web site, whether it knows where one is).</summary>
    public bool IsCached(string localPath) => _store.Has(localPath);

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
        return _store.Has(path) ? path : null;
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
        return _store.Has(path) ? path : null;
    }

    // Some faction logos are missing from the CB CDN (e.g. Hayabusa / 1199 returns 404); a copy bundled with the app,
    // when there is one, stands in.
    private LogoRequest FactionLogo(int factionId, string? url) =>
        new(GetCachedLogoPath(factionId), url, GetPackagedFactionLogoPath(factionId), AlwaysDownload: false);

    private LogoRequest UnitLogo(int factionId, int unitId, string? url) =>
        new(GetCachedUnitLogoPath(factionId, unitId), url, null, AlwaysDownload: factionId == TagCompanyFactionId && unitId == TagCompanyUnitId);

    // Fetches the logos MaxConcurrentDownloads at a time: one at a time, a first launch spent minutes waiting on the
    // network for thousands of small files. A null request is an item with no id, counted as having no logo.
    private async Task<LogoCacheResult> CacheAsync(IReadOnlyList<LogoRequest?> requests, CancellationToken cancellationToken)
    {
        var reused = 0;
        var downloaded = 0;
        var failed = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentDownloads, CancellationToken = cancellationToken };
        // An army can list a unit twice; two downloads must not write one file at once.
        var distinct = requests.OfType<LogoRequest>().DistinctBy(request => request.LocalPath, StringComparer.OrdinalIgnoreCase);
        await Parallel.ForEachAsync(distinct, options, async (request, token) =>
        {
            switch (await EnsureAvailableAsync(request, token))
            {
                case EnsureResult.Reused:
                    Interlocked.Increment(ref reused);
                    break;
                case EnsureResult.Downloaded:
                    Interlocked.Increment(ref downloaded);
                    break;
                default:
                    Interlocked.Increment(ref failed);
                    break;
            }
        });

        return new LogoCacheResult
        {
            TotalFactions = requests.Count,
            MissingLogoUrl = requests.Count(request => request is null),
            CachedReuse = reused,
            Downloaded = downloaded,
            Failed = failed
        };
    }

    private async Task<EnsureResult> EnsureAvailableAsync(LogoRequest request, CancellationToken cancellationToken)
    {
        if (!request.AlwaysDownload && _store.Has(request.LocalPath))
        {
            return EnsureResult.Reused;
        }

        if (IsPackagedAssetPath(request.Url))
        {
            return await _store.CopyPackagedAsync(request.Url, request.LocalPath, cancellationToken) ? EnsureResult.Downloaded : EnsureResult.NotAvailable;
        }

        if (!IsDownloadableUrl(request.Url))
        {
            return EnsureResult.NotAvailable;
        }

        if (await _store.FetchAsync(request.Url, request.LocalPath, cancellationToken))
        {
            return EnsureResult.Downloaded;
        }

        return request.BundledFallbackPath is { } fallback && await TryCopyBundledFallbackAsync(fallback, request.LocalPath, cancellationToken)
            ? EnsureResult.Downloaded
            : EnsureResult.NotAvailable;
    }

    private async Task<bool> TryCopyBundledFallbackAsync(string packagedPath, string localPath, CancellationToken cancellationToken)
    {
        bool exists;
        try
        {
            exists = await _packageFiles.ExistsAsync(packagedPath);
        }
        catch
        {
            exists = false;
        }

        return exists && await _store.CopyPackagedAsync(packagedPath, localPath, cancellationToken);
    }

    private static bool IsPackagedAssetPath([NotNullWhen(true)] string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && url.StartsWith(PackagedCacheRoot + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDownloadableUrl([NotNullWhen(true)] string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private enum EnsureResult
    {
        Reused,
        Downloaded,
        NotAvailable
    }
}
