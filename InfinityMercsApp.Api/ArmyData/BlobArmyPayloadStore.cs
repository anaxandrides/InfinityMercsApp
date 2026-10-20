using System.IO.Compression;
using System.Net;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace InfinityMercsApp.Api.ArmyData;

/// <inheritdoc/>
/// <remarks>Keeps each payload gzipped as payloads/{hash}.json.gz in the army data container, never overwritten.</remarks>
public sealed class BlobArmyPayloadStore(BlobServiceClient blobs) : IArmyPayloadStore
{
    /// <summary>The container, which the Bicep also creates.</summary>
    public const string Container = "army-data";

    private readonly BlobContainerClient _container = blobs.GetBlobContainerClient(Container);
    private bool _containerExists;

    public static string BlobName(string hash) => $"payloads/{hash}.json.gz";

    /// <inheritdoc/>
    public async Task SaveAsync(string hash, string json, CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);
        using var gzipped = new MemoryStream();
        await using (var gzip = new GZipStream(gzipped, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            await gzip.WriteAsync(Encoding.UTF8.GetBytes(json), cancellationToken);
        }

        gzipped.Position = 0;
        try
        {
            await _container.GetBlobClient(BlobName(hash)).UploadAsync(gzipped, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json", ContentEncoding = "gzip" },
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
            }, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status is (int)HttpStatusCode.Conflict or (int)HttpStatusCode.PreconditionFailed)
        {
            // Already kept: the same hash is the same payload.
        }
    }

    /// <inheritdoc/>
    public async Task<StoredPayload?> OpenAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            var download = await _container.GetBlobClient(BlobName(hash)).DownloadStreamingAsync(cancellationToken: cancellationToken);
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
