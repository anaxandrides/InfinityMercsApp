using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace InfinityMercsApp.Api.ArmyData.Logos;

/// <inheritdoc/>
/// <remarks>Keeps each bundle as logos/{hash}.zip in the army data container, beside the payloads.</remarks>
public sealed class BlobLogoBundleStore(BlobServiceClient blobs) : ILogoBundleStore
{
    private readonly BlobContainerClient _container = blobs.GetBlobContainerClient(BlobArmyPayloadStore.Container);

    public static string BlobName(string hash) => $"logos/{hash}.zip";

    /// <inheritdoc/>
    public async Task SaveAsync(string hash, Stream zip, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        try
        {
            await _container.GetBlobClient(BlobName(hash)).UploadAsync(zip, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/zip" },
                Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
            }, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status is (int)HttpStatusCode.Conflict or (int)HttpStatusCode.PreconditionFailed)
        {
            // Already kept: the same hash is the same logos.
        }
    }

    /// <inheritdoc/>
    public async Task<Stream?> OpenAsync(string hash, CancellationToken cancellationToken = default)
    {
        try
        {
            return (await _container.GetBlobClient(BlobName(hash)).DownloadStreamingAsync(cancellationToken: cancellationToken)).Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
