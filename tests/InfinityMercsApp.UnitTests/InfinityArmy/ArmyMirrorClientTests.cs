using System.IO.Compression;
using System.Net;
using System.Text;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;

namespace InfinityMercsApp.UnitTests.InfinityArmy;

public sealed class ArmyMirrorClientTests
{
    private static readonly ArmyMirrorEndpoint Endpoint = new(new Uri("https://mirror.example/api/"));

    private static ArmyMirrorClient ClientOver(RecordingHandler handler) => new(new HttpClient(handler), Endpoint);

    [Fact]
    public async Task GetManifestAsync_Served_ReadsIt()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, new StringContent(
            """{"checkedAt":"2026-10-20T06:00:00+00:00","metadata":{"hash":"m1","updatedAt":"2026-10-01T00:00:00+00:00"},"armies":[{"factionId":101,"version":"7.1","hash":"a1","updatedAt":"2026-10-01T00:00:00+00:00"}]}"""));

        var manifest = await ClientOver(handler).GetManifestAsync();

        Assert.NotNull(manifest);
        Assert.Equal("m1", manifest.Metadata?.Hash);
        Assert.Equal(new MirroredArmy(101, "7.1", "a1", new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)), Assert.Single(manifest.Armies));
        Assert.Equal("https://mirror.example/api/army/manifest", handler.Request?.RequestUri?.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "")]
    [InlineData(HttpStatusCode.OK, "<html>Starting</html>")]
    public async Task GetManifestAsync_NotServed_IsNull(HttpStatusCode status, string body)
    {
        var manifest = await ClientOver(new RecordingHandler(status, new StringContent(body))).GetManifestAsync();

        Assert.Null(manifest);
    }

    [Fact]
    public async Task GetArmyAsync_GzippedAnswer_IsUnzipped()
    {
        using var gzipped = new MemoryStream();
        using (var gzip = new GZipStream(gzipped, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(Encoding.UTF8.GetBytes("""{"version":"7.1"}"""));
        }

        var content = new ByteArrayContent(gzipped.ToArray());
        content.Headers.ContentEncoding.Add("gzip");
        var handler = new RecordingHandler(HttpStatusCode.OK, content);

        var payload = await ClientOver(handler).GetArmyAsync(101);

        Assert.Equal("""{"version":"7.1"}""", payload);
        Assert.Equal("https://mirror.example/api/army/units/101", handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetMetadataAsync_NotServed_ThrowsWithTheStatus()
    {
        var client = ClientOver(new RecordingHandler(HttpStatusCode.NotFound, new StringContent(string.Empty)));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMetadataAsync());

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task GetManifestAsync_Unreachable_IsNull() =>
        Assert.Null(await new ArmyMirrorClient(new HttpClient(new ThrowingHandler()), Endpoint).GetManifestAsync());

    [Fact]
    public async Task GetListingsAsync_Served_ReadsTheListingsRoute()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, new StringContent("{}"));

        Assert.Equal("{}", await ClientOver(handler).GetListingsAsync());
        Assert.Equal("https://mirror.example/api/army/listings", handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetGeneratedArmyAsync_Served_ReadsTheFactionsRoute()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, new StringContent("{}"));

        Assert.Equal("{}", await ClientOver(handler).GetGeneratedArmyAsync(2001));
        Assert.Equal("https://mirror.example/api/army/generated/2001", handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task DownloadLogoBundleAsync_Served_WritesTheFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"logos-{Guid.NewGuid():N}.zip");
        try
        {
            var downloaded = await ClientOver(new RecordingHandler(HttpStatusCode.OK, new ByteArrayContent([1, 2, 3]))).DownloadLogoBundleAsync(path);

            Assert.True(downloaded);
            Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DownloadLogoBundleAsync_NotServed_IsFalseAndWritesNothing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"logos-{Guid.NewGuid():N}.zip");

        var downloaded = await ClientOver(new RecordingHandler(HttpStatusCode.NotFound, new StringContent(string.Empty))).DownloadLogoBundleAsync(path);

        Assert.False(downloaded);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task DownloadLogoBundleAsync_Unreachable_IsFalse() =>
        Assert.False(await new ArmyMirrorClient(new HttpClient(new ThrowingHandler()), Endpoint).DownloadLogoBundleAsync("unused.zip"));

    [Fact]
    public async Task DownloadLogoBundleAsync_FolderMissing_IsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}", "logos.zip");

        Assert.False(await ClientOver(new RecordingHandler(HttpStatusCode.OK, new ByteArrayContent([1]))).DownloadLogoBundleAsync(path));
    }
}
