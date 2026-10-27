using System.Text.Json;
using InfinityMercsApp.Api.ArmyData.Logos;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Net.Http.Headers;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>
/// The mirrored army data, open to anyone: the current snapshot's manifest and payloads, and those of any earlier
/// snapshot, which a company pins.
/// </summary>
public sealed class ArmyDataFunctions(IArmySnapshotStore snapshots, IArmyPayloadStore payloads, ILogoBundleStore logoBundles)
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

    /// <summary>
    /// Every faction's and unit's logo in one zip, for an app's first launch; its ETag is the bundle's hash, which the
    /// manifest names.
    /// </summary>
    [Function("GetArmyLogos")]
    public async Task<IActionResult> GetLogosAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/logos")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await snapshots.ReadLatestAsync(cancellationToken) is not { Logos.Hash: { } hash } ||
            await logoBundles.OpenAsync(hash, cancellationToken) is not { } zip)
        {
            return new NotFoundResult();
        }

        var etag = new EntityTagHeaderValue($"\"{hash}\"");
        var response = request.HttpContext.Response;
        response.Headers.ETag = etag.ToString();
        response.Headers.CacheControl = PayloadResponses.CurrentCacheControl;
        if (request.GetTypedHeaders().IfNoneMatch.Any(match => match.Compare(etag, useStrongComparison: false)))
        {
            await zip.DisposeAsync();
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        return new FileStreamResult(zip, "application/zip");
    }

    /// <summary>
    /// A generated company's faction (Airborne, Inspiring, TAG) as the current snapshot's armies build it
    /// (ArmyDefinitionJson), for the web site, which reads it rather than build it in the browser.
    /// </summary>
    [Function("GetGeneratedArmy")]
    public async Task<IActionResult> GetGeneratedArmyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/generated/{factionId:int}")] HttpRequest request,
        int factionId,
        CancellationToken cancellationToken)
    {
        var hash = (await snapshots.ReadLatestAsync(cancellationToken))?.Generated.FirstOrDefault(army => army.FactionId == factionId)?.Hash;
        return PayloadResponses.For(request, hash is null ? null : await payloads.OpenAsync(hash, cancellationToken), PayloadResponses.CurrentCacheControl);
    }

    /// <summary>
    /// Which factions the Airborne, Inspiring and Cohesive companies list (FactionListings), worked out from the current
    /// snapshot's armies, so the web site needn't read every army to list them.
    /// </summary>
    [Function("GetArmyListings")]
    public async Task<IActionResult> GetListingsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/listings")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var hash = (await snapshots.ReadLatestAsync(cancellationToken))?.Listings?.Hash;
        return PayloadResponses.For(request, hash is null ? null : await payloads.OpenAsync(hash, cancellationToken), PayloadResponses.CurrentCacheControl);
    }

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
