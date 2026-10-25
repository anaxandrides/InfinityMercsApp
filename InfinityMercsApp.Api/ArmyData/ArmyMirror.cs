using System.Collections.Concurrent;
using System.Text.Json;
using Azure;
using InfinityMercsApp.Api.ArmyData.Generated;
using InfinityMercsApp.Api.ArmyData.Logos;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using Microsoft.Extensions.Logging;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>
/// Copies Corvus Belli's army data, so the app and the web site read it from here: the metadata and each faction's
/// army, kept exactly as served under their hashes. Each distinct set of them is a snapshot, kept for good. The logos
/// they name are bundled into one zip whenever they change, and the generated companies' factions are built from them.
/// </summary>
public sealed class ArmyMirror(
    IArmyPayloadSource source,
    IArmyPayloadStore payloads,
    IArmySnapshotStore snapshots,
    LogoBundler logoBundler,
    GeneratedArmyBuilder generatedArmies,
    TimeProvider time,
    ILogger<ArmyMirror> logger)
{
    private const int ConcurrentDownloads = 4;

    /// <summary>
    /// Downloads the metadata and every army it lists and keeps those that changed. An army that fails keeps its
    /// earlier copy; a faction no longer listed is left out. When anything changed, the result is a new snapshot.
    /// </summary>
    /// <exception cref="HttpRequestException">The metadata couldn't be downloaded; nothing changes.</exception>
    /// <exception cref="JsonException">The metadata wasn't readable; nothing changes.</exception>
    public async Task<ArmyMirrorRefresh> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var previous = await snapshots.ReadLatestAsync(cancellationToken) ?? new ArmyMirrorManifest();

        var metadataJson = await source.GetMetadataAsync(cancellationToken);
        var factionIds = ArmyPayloads.FactionIds(metadataJson);
        var metadataHash = ArmyPayloads.Hash(metadataJson);
        var metadata = previous.Metadata;
        var metadataUpdated = metadata?.Hash != metadataHash;
        if (metadataUpdated)
        {
            await payloads.SaveAsync(metadataHash, metadataJson, cancellationToken);
            metadata = new MirroredPayload(metadataHash, now);
        }

        var previousArmies = previous.Armies.ToDictionary(army => army.FactionId);
        var updated = new ConcurrentBag<int>();
        var failed = new ConcurrentBag<int>();
        var armyLogos = new ConcurrentBag<LogoFile>();
        var armyJson = new ConcurrentDictionary<int, string>();
        using var gate = new SemaphoreSlim(ConcurrentDownloads);
        var armies = await Task.WhenAll(factionIds.Select(async factionId =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var kept = previousArmies.GetValueOrDefault(factionId);
                var copied = await CopyAsync(factionId, kept, now, armyLogos, armyJson, cancellationToken);
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

        var kept = armies.OfType<MirroredArmy>().OrderBy(army => army.FactionId).ToList();
        var logos = failed.IsEmpty
            ? await BundleLogosAsync(previous.Logos, [.. ArmyLogos.OfMetadata(metadataJson), .. armyLogos], now, cancellationToken)
            : previous.Logos;
        var snapshotId = ArmySnapshots.IdFor(metadata, kept);
        var generated = failed.IsEmpty && NeedsGenerating(previous, snapshotId)
            ? await BuildGeneratedAsync(previous, new CopiedPayloads(metadataJson, armyJson), now, cancellationToken)
            : previous;
        var manifest = new ArmyMirrorManifest
        {
            SnapshotId = snapshotId,
            CheckedAt = now,
            Metadata = metadata,
            Armies = kept,
            Logos = logos,
            Generated = generated.Generated,
            GeneratedRulesVersion = generated.GeneratedRulesVersion
        };

        // The snapshot is kept before it becomes current, so the current one can always be read by its id.
        var newSnapshot = manifest.SnapshotId != previous.SnapshotId;
        if (newSnapshot)
        {
            await snapshots.SaveAsync(manifest, cancellationToken);
        }

        await snapshots.SetLatestAsync(manifest, cancellationToken);
        return new ArmyMirrorRefresh(
            manifest.SnapshotId,
            newSnapshot,
            metadataUpdated,
            updated.Order().ToList(),
            failed.Order().ToList(),
            logos != previous.Logos,
            !ReferenceEquals(generated, previous));
    }

    // The generated factions follow from the armies and the rules that build them, so they are built again when either
    // changed, or when there are none yet.
    private static bool NeedsGenerating(ArmyMirrorManifest previous, string snapshotId) =>
        previous.Generated.Count == 0 ||
        previous.SnapshotId != snapshotId ||
        previous.GeneratedRulesVersion != GeneratedCompanies.RulesVersion;

    // The manifest with newly built generated factions; the previous one when building failed, so they are kept.
    private async Task<ArmyMirrorManifest> BuildGeneratedAsync(
        ArmyMirrorManifest previous, CopiedPayloads copied, DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            var built = await generatedArmies.BuildAsync(copied, now, cancellationToken);
            logger.LogInformation("Generated factions built: {Factions}.", string.Join(", ", built.Select(army => army.FactionId)));
            return previous with { Generated = built, GeneratedRulesVersion = GeneratedCompanies.RulesVersion };
        }
        catch (Exception ex) when (ex is RequestFailedException or IOException or InvalidDataException or JsonException)
        {
            logger.LogWarning(ex, "The generated factions couldn't be built; the current ones stay.");
            return previous;
        }
    }

    // The bundle of these logos: the current one when they haven't changed, else a new one. Built only when every army
    // was read, so a failed download never ships a bundle missing that army's units; if building fails, the current
    // bundle stays.
    private async Task<MirroredPayload?> BundleLogosAsync(
        MirroredPayload? current, IReadOnlyList<LogoFile> logos, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var hash = ArmyLogos.Hash(logos);
        if (current?.Hash == hash)
        {
            return current;
        }

        try
        {
            var bundled = await logoBundler.BuildAsync(hash, logos, cancellationToken);
            logger.LogInformation("Logo bundle {Hash} built with {Bundled} of {Listed} logos.", hash, bundled, logos.Count);
            return new MirroredPayload(hash, now);
        }
        catch (Exception ex) when (ex is RequestFailedException or IOException)
        {
            logger.LogWarning(ex, "The logo bundle couldn't be kept; the current one stays.");
            return current;
        }
    }

    // The kept copy when the payload is unchanged, a new one when it changed, or null when it couldn't be downloaded.
    // Its units' logos go into armyLogos, and the payload into armyJson.
    private async Task<MirroredArmy?> CopyAsync(
        int factionId,
        MirroredArmy? kept,
        DateTimeOffset now,
        ConcurrentBag<LogoFile> armyLogos,
        ConcurrentDictionary<int, string> armyJson,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await source.GetArmyAsync(factionId, cancellationToken);
            if (ArmyPayloads.Version(json) is not { } version)
            {
                logger.LogWarning("The army of faction {FactionId} came back without a version.", factionId);
                return null;
            }

            armyJson[factionId] = json;
            foreach (var logo in ArmyLogos.OfArmy(factionId, json))
            {
                armyLogos.Add(logo);
            }

            var hash = ArmyPayloads.Hash(json);
            if (kept?.Hash == hash)
            {
                return kept;
            }

            await payloads.SaveAsync(hash, json, cancellationToken);
            return new MirroredArmy(factionId, version, hash, now);
        }
        catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "The army of faction {FactionId} couldn't be copied.", factionId);
            return null;
        }
    }
}
