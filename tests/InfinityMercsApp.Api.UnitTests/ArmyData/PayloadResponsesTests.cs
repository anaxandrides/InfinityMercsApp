using System.IO.Compression;
using System.Text;
using InfinityMercsApp.Api.ArmyData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

public sealed class PayloadResponsesTests
{
    private const string Json = """{"version":"1.0"}""";
    private const string Hash = "abc123";

    private static StoredPayload Payload()
    {
        var gzipped = new MemoryStream();
        using (var gzip = new GZipStream(gzipped, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(Encoding.UTF8.GetBytes(Json));
        }

        gzipped.Position = 0;
        return new StoredPayload(gzipped, Hash);
    }

    private static HttpRequest Request(string? acceptEncoding = null, string? ifNoneMatch = null)
    {
        var context = new DefaultHttpContext();
        if (acceptEncoding is not null)
        {
            context.Request.Headers.AcceptEncoding = acceptEncoding;
        }

        if (ifNoneMatch is not null)
        {
            context.Request.Headers.IfNoneMatch = ifNoneMatch;
        }

        return context.Request;
    }

    private static string Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void For_NoPayload_IsNotFound() => Assert.IsType<NotFoundResult>(PayloadResponses.For(Request(), null));

    [Fact]
    public void For_ClientHasTheSameCopy_IsNotModified()
    {
        var request = Request("gzip", $"\"{Hash}\"");

        var result = PayloadResponses.For(request, Payload());

        Assert.Equal(StatusCodes.Status304NotModified, Assert.IsType<StatusCodeResult>(result).StatusCode);
        Assert.Equal($"\"{Hash}\"", request.HttpContext.Response.Headers.ETag.ToString());
    }

    [Fact]
    public void For_ClientHasAnOlderCopy_SendsThePayload()
    {
        var result = PayloadResponses.For(Request("gzip", "\"older\""), Payload());

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public void For_ClientTakesGzip_SendsItGzipped()
    {
        var request = Request("gzip, deflate, br");

        var result = Assert.IsType<FileStreamResult>(PayloadResponses.For(request, Payload()));

        var response = request.HttpContext.Response;
        Assert.Equal("gzip", response.Headers.ContentEncoding.ToString());
        Assert.Equal(PayloadResponses.CacheControl, response.Headers.CacheControl.ToString());
        Assert.Equal("application/json", result.ContentType);
        Assert.Equal(Json, Read(new GZipStream(result.FileStream, CompressionMode.Decompress)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("br")]
    [InlineData("gzip;q=0")]
    public void For_ClientDoesNotTakeGzip_SendsItUnzipped(string? acceptEncoding)
    {
        var request = Request(acceptEncoding);

        var result = Assert.IsType<FileStreamResult>(PayloadResponses.For(request, Payload()));

        Assert.Empty(request.HttpContext.Response.Headers.ContentEncoding.ToString());
        Assert.Equal(Json, Read(result.FileStream));
    }
}
