using System.Text.Json;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>
/// The mirrored army data, open to anyone: the current snapshot's manifest and payloads, and those of any earlier
/// snapshot, which a company pins.
/// </summary>
public sealed class ArmyDataFunctions(IArmySnapshotStore snapshots, IArmyPayloadStore payloads)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The current snapshot's manifest, which clients check first to see what changed; never cached.</summary>
    [Function("GetArmyManifest")]
    public async Task<IActionResult> GetManifestAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/manifest")] HttpRequest request,
        CancellationToken cancellationToken) =>
        Manifest(request, await snapshots.ReadLatestAsync(cancellationToken), "no-cache");

    [Function("GetArmyMetadata")]
    public async Task<IActionResult> GetMetadataAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/metadata")] HttpRequest request,
        CancellationToken cancellationToken) =>
        await PayloadAsync(request, await snapshots.ReadLatestAsync(cancellationToken), PayloadRequest.Metadata, PayloadResponses.CurrentCacheControl, cancellationToken);

    [Function("GetArmy")]
    public async Task<IActionResult> GetArmyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/units/{factionId:int}")] HttpRequest request,
        int factionId,
        CancellationToken cancellationToken) =>
        await PayloadAsync(request, await snapshots.ReadLatestAsync(cancellationToken), PayloadRequest.Army(factionId), PayloadResponses.CurrentCacheControl, cancellationToken);

    /// <summary>A snapshot's manifest; it never changes.</summary>
    [Function("GetArmySnapshot")]
    public async Task<IActionResult> GetSnapshotAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/snapshots/{snapshotId}")] HttpRequest request,
        string snapshotId,
        CancellationToken cancellationToken) =>
        Manifest(request, await SnapshotAsync(snapshotId, cancellationToken), PayloadResponses.SnapshotCacheControl);

    [Function("GetArmySnapshotMetadata")]
    public async Task<IActionResult> GetSnapshotMetadataAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/snapshots/{snapshotId}/metadata")] HttpRequest request,
        string snapshotId,
        CancellationToken cancellationToken) =>
        await PayloadAsync(request, await SnapshotAsync(snapshotId, cancellationToken), PayloadRequest.Metadata, PayloadResponses.SnapshotCacheControl, cancellationToken);

    [Function("GetArmySnapshotArmy")]
    public async Task<IActionResult> GetSnapshotArmyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/snapshots/{snapshotId}/units/{factionId:int}")] HttpRequest request,
        string snapshotId,
        int factionId,
        CancellationToken cancellationToken) =>
        await PayloadAsync(request, await SnapshotAsync(snapshotId, cancellationToken), PayloadRequest.Army(factionId), PayloadResponses.SnapshotCacheControl, cancellationToken);

    // Anything that isn't shaped like a snapshot id isn't looked up.
    private async Task<ArmyMirrorManifest?> SnapshotAsync(string snapshotId, CancellationToken cancellationToken) =>
        ArmySnapshots.IsId(snapshotId) ? await snapshots.ReadAsync(snapshotId, cancellationToken) : null;

    private static IActionResult Manifest(HttpRequest request, ArmyMirrorManifest? manifest, string cacheControl)
    {
        if (manifest is null)
        {
            return new NotFoundResult();
        }

        request.HttpContext.Response.Headers.CacheControl = cacheControl;
        return new JsonResult(manifest, JsonOptions);
    }

    private async Task<IActionResult> PayloadAsync(
        HttpRequest request,
        ArmyMirrorManifest? snapshot,
        PayloadRequest payload,
        string cacheControl,
        CancellationToken cancellationToken)
    {
        var hash = snapshot is null ? null : ArmySnapshots.HashOf(snapshot, payload);
        return PayloadResponses.For(request, hash is null ? null : await payloads.OpenAsync(hash, cancellationToken), cacheControl);
    }
}
