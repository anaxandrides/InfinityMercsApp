using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Api.ArmyData.Logos;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.ArmyData.Logos;

public sealed class BlobLogoBundleStoreTests
{
    private readonly BlobClient _blob = Substitute.For<BlobClient>();
    private readonly BlobLogoBundleStore _store;

    public BlobLogoBundleStoreTests()
    {
        var blobs = Substitute.For<BlobServiceClient>();
        var container = Substitute.For<BlobContainerClient>();
        blobs.GetBlobContainerClient(BlobArmyPayloadStore.Container).Returns(container);
        container.GetBlobClient("logos/abc.zip").Returns(_blob);
        _store = new BlobLogoBundleStore(blobs);
    }

    [Fact]
    public async Task SaveAsync_Bundle_UploadsItAsAZipWithoutOverwriting()
    {
        using var zip = new MemoryStream([1]);

        await _store.SaveAsync("abc", zip);

        await _blob.Received(1).UploadAsync(
            zip,
            Arg.Is<BlobUploadOptions>(options => options.HttpHeaders.ContentType == "application/zip" && options.Conditions.IfNoneMatch == ETag.All),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_AlreadyKept_IsFine()
    {
        _blob.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns<Response<BlobContentInfo>>(_ => throw new RequestFailedException(412, "Exists."));

        await _store.SaveAsync("abc", new MemoryStream([1]));
    }

    [Fact]
    public async Task OpenAsync_Kept_StreamsIt()
    {
        var content = new MemoryStream([1, 2]);
        _blob.DownloadStreamingAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(content), Substitute.For<Response>()));

        Assert.Same(content, await _store.OpenAsync("abc"));
    }

    [Fact]
    public async Task OpenAsync_NotKept_IsNull()
    {
        _blob.DownloadStreamingAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns<Response<BlobDownloadStreamingResult>>(_ => throw new RequestFailedException(404, "Missing."));

        Assert.Null(await _store.OpenAsync("abc"));
    }
}
