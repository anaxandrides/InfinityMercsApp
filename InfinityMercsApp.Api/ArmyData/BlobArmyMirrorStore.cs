using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;

namespace InfinityMercsApp.Api.ArmyData;

/// <inheritdoc/>
/// <remarks>Keeps everything in the <see cref="MirrorBlobNames.Container"/> blob container, payloads gzipped.</remarks>
public sealed class BlobArmyMirrorStore(BlobServiceClient blobs) : IArmyMirrorStore
{
    private const string HashKey = "sha256";
    private const string JsonType = "application/json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly BlobContainerClient _container = blobs.GetBlobContainerClient(MirrorBlobNames.Container);
    private bool _containerExists;

    /// <inheritdoc/>
    public async Task<ArmyMirrorManifest?> ReadManifestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await _container.GetBlobClient(MirrorBlobNames.Manifest).DownloadContentAsync(cancellationToken);
            return content.Value.Content.ToObjectFromJson<ArmyMirrorManifest>(JsonOptions);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task WriteManifestAsync(ArmyMirrorManifest manifest, CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);
        await _container.GetBlobClient(MirrorBlobNames.Manifest).UploadAsync(
            BinaryData.FromObjectAsJson(manifest, JsonOptions),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = JsonType } },
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WritePayloadAsync(string name, string json, string hash, CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);
        using var gzipped = new MemoryStream();
        await using (var gzip = new GZipStream(gzipped, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            await gzip.WriteAsync(Encoding.UTF8.GetBytes(json), cancellationToken);
        }

        gzipped.Position = 0;
        await _container.GetBlobClient(name).UploadAsync(gzipped, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = JsonType, ContentEncoding = "gzip" },
            Metadata = new Dictionary<string, string> { [HashKey] = hash }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<StoredPayload?> OpenPayloadAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var download = await _container.GetBlobClient(name).DownloadStreamingAsync(cancellationToken: cancellationToken);
            var hash = download.Value.Details.Metadata.TryGetValue(HashKey, out var value) ? value : string.Empty;
            return new StoredPayload(download.Value.Content, hash);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // The Bicep creates the container; this covers Azurite when running locally.
    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (!_containerExists)
        {
            await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            _containerExists = true;
        }
    }
}
