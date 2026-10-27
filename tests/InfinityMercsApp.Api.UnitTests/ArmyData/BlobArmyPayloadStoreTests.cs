using System.IO.Compression;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InfinityMercsApp.Api.ArmyData;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

/// <summary>Payloads are kept gzipped under their hash and never overwritten.</summary>
public sealed class BlobArmyPayloadStoreTests
{
    private readonly BlobContainerClient _container = Substitute.For<BlobContainerClient>();
    private readonly BlobClient _blob = Substitute.For<BlobClient>();
    private readonly BlobArmyPayloadStore _store;
    private byte[]? _uploaded;
    private BlobUploadOptions? _options;

    public BlobArmyPayloadStoreTests()
    {
        var blobs = Substitute.For<BlobServiceClient>();
        blobs.GetBlobContainerClient(BlobArmyPayloadStore.Container).Returns(_container);
        _container.GetBlobClient("payloads/abc.json.gz").Returns(_blob);
        _blob.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                using var copy = new MemoryStream();
                call.Arg<Stream>().CopyTo(copy);
                _uploaded = copy.ToArray();
                _options = call.Arg<BlobUploadOptions>();
                return Task.FromResult(Substitute.For<Response<BlobContentInfo>>());
            });
        _store = new BlobArmyPayloadStore(blobs);
    }

    [Fact]
    public async Task SaveAsync_Payload_UploadsItGzippedWithoutOverwriting()
    {
        await _store.SaveAsync("abc", """{"version":"7"}""");

        using var gzip = new GZipStream(new MemoryStream(_uploaded ?? []), CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        Assert.Equal("""{"version":"7"}""", await reader.ReadToEndAsync());
        Assert.Equal("gzip", _options?.HttpHeaders.ContentEncoding);
        Assert.Equal(ETag.All, _options?.Conditions.IfNoneMatch);
    }

    [Fact]
    public async Task SaveAsync_Twice_CreatesTheContainerOnce()
    {
        await _store.SaveAsync("abc", "{}");
        await _store.SaveAsync("abc", "{}");

        Assert.Single(_container.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(BlobContainerClient.CreateIfNotExistsAsync));
    }

    [Theory]
    [InlineData(409)]
    [InlineData(412)]
    public async Task SaveAsync_AlreadyKept_IsFine(int status)
    {
        _blob.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns<Response<BlobContentInfo>>(_ => throw new RequestFailedException(status, "Exists."));

        await _store.SaveAsync("abc", "{}");
    }

    [Fact]
    public async Task SaveAsync_StorageFails_Throws()
    {
        _blob.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobUploadOptions>(), Arg.Any<CancellationToken>())
            .Returns<Response<BlobContentInfo>>(_ => throw new RequestFailedException(500, "Down."));

        await Assert.ThrowsAsync<RequestFailedException>(() => _store.SaveAsync("abc", "{}"));
    }

    [Fact]
    public async Task OpenAsync_Kept_StreamsIt()
    {
        var content = new MemoryStream([1, 2]);
        _blob.DownloadStreamingAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(content), Substitute.For<Response>()));

        var payload = await _store.OpenAsync("abc");

        Assert.Same(content, payload?.GzippedContent);
        Assert.Equal("abc", payload?.Hash);
    }

    [Fact]
    public async Task OpenAsync_NotKept_IsNull()
    {
        _blob.DownloadStreamingAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns<Response<BlobDownloadStreamingResult>>(_ => throw new RequestFailedException(404, "Missing."));

        Assert.Null(await _store.OpenAsync("abc"));
    }

    [Fact]
    public void BlobName_Hash_IsUnderPayloads() => Assert.Equal("payloads/abc.json.gz", BlobArmyPayloadStore.BlobName("abc"));
}
