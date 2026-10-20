using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>The mirrored army data, open to anyone: the manifest, the metadata and each faction's army.</summary>
public sealed class ArmyDataFunctions(IArmyMirrorStore store)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The manifest, which clients check first to see what changed; never cached.</summary>
    [Function("GetArmyManifest")]
    public async Task<IActionResult> GetManifestAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/manifest")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await store.ReadManifestAsync(cancellationToken) is not { } manifest)
        {
            return new NotFoundResult();
        }

        request.HttpContext.Response.Headers.CacheControl = "no-cache";
        return new JsonResult(manifest, JsonOptions);
    }

    [Function("GetArmyMetadata")]
    public async Task<IActionResult> GetMetadataAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/metadata")] HttpRequest request,
        CancellationToken cancellationToken) =>
        PayloadResponses.For(request, await store.OpenPayloadAsync(MirrorBlobNames.Metadata, cancellationToken));

    [Function("GetArmy")]
    public async Task<IActionResult> GetArmyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "army/units/{factionId:int}")] HttpRequest request,
        int factionId,
        CancellationToken cancellationToken) =>
        PayloadResponses.For(request, await store.OpenPayloadAsync(MirrorBlobNames.Army(factionId), cancellationToken));
}
