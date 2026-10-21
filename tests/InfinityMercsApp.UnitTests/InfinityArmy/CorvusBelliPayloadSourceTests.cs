using System.IO.Compression;
using System.Net;
using System.Text;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;

namespace InfinityMercsApp.UnitTests.InfinityArmy;

public sealed class CorvusBelliPayloadSourceTests
{
    private const string Json = """{"version":"1.0"}""";

    private static CorvusBelliPayloadSource SourceOver(RecordingHandler handler) => new(new HttpClient(handler));

    private static ByteArrayContent Gzipped(string text)
    {
        using var gzipped = new MemoryStream();
        using (var gzip = new GZipStream(gzipped, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(Encoding.UTF8.GetBytes(text));
        }

        var content = new ByteArrayContent(gzipped.ToArray());
        content.Headers.ContentEncoding.Add("gzip");
        return content;
    }

    [Fact]
    public async Task GetArmyAsync_Faction_AsksForItWithCorvusBellisOrigin()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, new StringContent(Json));

        var payload = await SourceOver(handler).GetArmyAsync(101);

        Assert.Equal(Json, payload);
        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal(CorvusBelliHttp.ArmyUrl(101), request.RequestUri?.ToString());
        Assert.Equal("https://infinityuniverse.com", Assert.Single(request.Headers.GetValues("Origin")));
    }

    [Fact]
    public async Task GetMetadataAsync_GzippedAnswer_IsUnzipped()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, Gzipped(Json));

        var payload = await SourceOver(handler).GetMetadataAsync();

        Assert.Equal(Json, payload);
        Assert.Equal(CorvusBelliHttp.MetadataUrl, handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetArmyAsync_Refused_ThrowsWithTheStatus()
    {
        var source = SourceOver(new RecordingHandler(HttpStatusCode.Forbidden, new StringContent("Forbidden")));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => source.GetArmyAsync(101));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }
}
