using System.Collections.Concurrent;
using System.Text.Json;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using Microsoft.Extensions.Logging;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>
/// Copies Corvus Belli's army data, so the app and the web site read it from here: the metadata and each faction's
/// army, kept exactly as served, with a manifest of their hashes.
/// </summary>
public sealed class ArmyMirror(IArmyPayloadSource source, IArmyMirrorStore store, TimeProvider time, ILogger<ArmyMirror> logger)
{
    private const int ConcurrentDownloads = 4;

    /// <summary>
    /// Downloads the metadata and every army it lists and keeps those that changed. An army that fails keeps its
    /// earlier copy; a faction no longer listed is dropped from the manifest.
    /// </summary>
    /// <exception cref="HttpRequestException">The metadata couldn't be downloaded; nothing changes.</exception>
    /// <exception cref="JsonException">The metadata wasn't readable; nothing changes.</exception>
    public async Task<ArmyMirrorRefresh> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var previous = await store.ReadManifestAsync(cancellationToken) ?? new ArmyMirrorManifest();

        var metadataJson = await source.GetMetadataAsync(cancellationToken);
        var factionIds = ArmyPayloads.FactionIds(metadataJson);
        var metadataHash = ArmyPayloads.Hash(metadataJson);
        var metadata = previous.Metadata;
        var metadataUpdated = metadata?.Hash != metadataHash;
        if (metadataUpdated)
        {
            await store.WritePayloadAsync(MirrorBlobNames.Metadata, metadataJson, metadataHash, cancellationToken);
            metadata = new MirroredPayload(metadataHash, now);
        }

        var previousArmies = previous.Armies.ToDictionary(army => army.FactionId);
        var updated = new ConcurrentBag<int>();
        var failed = new ConcurrentBag<int>();
        using var gate = new SemaphoreSlim(ConcurrentDownloads);
        var armies = await Task.WhenAll(factionIds.Select(async factionId =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var kept = previousArmies.GetValueOrDefault(factionId);
                var copied = await CopyAsync(factionId, kept, now, cancellationToken);
                if (copied is null)
                {
                    failed.Add(factionId);
                    return kept;
                }

                if (!ReferenceEquals(copied, kept))
                {
                    updated.Add(factionId);
                }

                return copied;
            }
            finally
            {
                gate.Release();
            }
        }));

        await store.WriteManifestAsync(new ArmyMirrorManifest
        {
            CheckedAt = now,
            Metadata = metadata,
            Armies = armies.OfType<MirroredArmy>().OrderBy(army => army.FactionId).ToList()
        }, cancellationToken);

        return new ArmyMirrorRefresh(metadataUpdated, updated.Order().ToList(), failed.Order().ToList());
    }

    // The kept copy when the payload is unchanged, a new one when it changed, or null when it couldn't be downloaded.
    private async Task<MirroredArmy?> CopyAsync(int factionId, MirroredArmy? kept, DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            var json = await source.GetArmyAsync(factionId, cancellationToken);
            if (ArmyPayloads.Version(json) is not { } version)
            {
                logger.LogWarning("The army of faction {FactionId} came back without a version.", factionId);
                return null;
            }

            var hash = ArmyPayloads.Hash(json);
            if (kept?.Hash == hash)
            {
                return kept;
            }

            await store.WritePayloadAsync(MirrorBlobNames.Army(factionId), json, hash, cancellationToken);
            return new MirroredArmy(factionId, version, hash, now);
        }
        catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "The army of faction {FactionId} couldn't be copied.", factionId);
            return null;
        }
    }
}
