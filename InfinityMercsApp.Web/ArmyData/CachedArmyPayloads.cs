using System.Collections.Concurrent;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;

namespace InfinityMercsApp.Web.ArmyData;

/// <summary>
/// The army data's payloads for the site: from the browser's cache when the current snapshot's payload is kept there,
/// otherwise from the API's copy (then kept). Corvus Belli's own API doesn't answer browsers on other sites. Armies can
/// be fetched ahead, several at once, and are held until they are stored.
/// </summary>
public sealed class CachedArmyPayloads(ArmyMirrorClient mirror, BrowserPayloadCache cache) : IArmyPayloadSource
{
    // Enough to hide the network's latency without crowding the API.
    private const int ConcurrentDownloads = 6;

    private readonly ConcurrentDictionary<int, Task<string>> _armies = new();
    private readonly SemaphoreSlim _downloads = new(ConcurrentDownloads);
    private ArmyMirrorManifest _manifest = new();

    /// <summary>How many payloads came from the browser's cache rather than the API.</summary>
    public int CacheHits { get; private set; }

    /// <summary>The snapshot whose payloads are read, which names each payload's hash.</summary>
    public void Use(ArmyMirrorManifest manifest) => _manifest = manifest;

    public async Task<string> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        var hash = _manifest.Metadata?.Hash;
        return await KeptAsync(hash) ?? await KeepAsync(hash, await mirror.GetMetadataAsync(cancellationToken));
    }

    /// <summary>The army, fetched now unless it was fetched ahead.</summary>
    public Task<string> GetArmyAsync(int factionId, CancellationToken cancellationToken = default) =>
        _armies.GetOrAdd(factionId, ReadArmyAsync);

    /// <summary>Starts fetching these armies, several at a time, so they are ready when asked for.</summary>
    public void FetchAhead(IEnumerable<int> factionIds)
    {
        foreach (var factionId in factionIds)
        {
            _armies.GetOrAdd(factionId, ReadArmyAsync);
        }
    }

    /// <summary>Lets go of an army once it is stored (or failed), so a later ask fetches it again.</summary>
    public void Release(int factionId) => _armies.TryRemove(factionId, out _);

    /// <summary>A generated company's faction as the API built it.</summary>
    public async Task<string> GetGeneratedArmyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        var hash = _manifest.Generated.FirstOrDefault(army => army.FactionId == factionId)?.Hash;
        return await KeptAsync(hash) ?? await KeepAsync(hash, await mirror.GetGeneratedArmyAsync(factionId, cancellationToken));
    }

    private async Task<string> ReadArmyAsync(int factionId)
    {
        var hash = _manifest.Armies.FirstOrDefault(army => army.FactionId == factionId)?.Hash;
        if (await KeptAsync(hash) is { } kept)
        {
            return kept;
        }

        await _downloads.WaitAsync();
        try
        {
            return await KeepAsync(hash, await mirror.GetArmyAsync(factionId));
        }
        finally
        {
            _downloads.Release();
        }
    }

    // The payload kept in the browser under the hash; null when the hash is unknown or nothing is kept.
    private async Task<string?> KeptAsync(string? hash)
    {
        if (hash is null || await cache.GetAsync(hash) is not { } kept)
        {
            return null;
        }

        CacheHits++;
        return kept;
    }

    private async Task<string> KeepAsync(string? hash, string json)
    {
        if (hash is not null)
        {
            await cache.PutAsync(hash, json);
        }

        return json;
    }
}
