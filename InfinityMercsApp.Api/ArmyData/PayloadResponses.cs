using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>How a mirrored payload goes back to a client: gzipped when it takes gzip, and not at all when its copy is current.</summary>
public static class PayloadResponses
{
    /// <summary>For the current army data: clients may reuse it for an hour, and asking again is cheap with the ETag.</summary>
    public const string CurrentCacheControl = "public, max-age=3600";

    /// <summary>For a snapshot's army data, which never changes.</summary>
    public const string SnapshotCacheControl = "public, max-age=31536000, immutable";

    public static IActionResult For(HttpRequest request, StoredPayload? payload, string cacheControl)
    {
        if (payload is null)
        {
            return new NotFoundResult();
        }

        var etag = new EntityTagHeaderValue($"\"{payload.Hash}\"");
        var response = request.HttpContext.Response;
        response.Headers.ETag = etag.ToString();
        response.Headers.CacheControl = cacheControl;
        response.Headers.Vary = HeaderNames.AcceptEncoding;
        if (request.GetTypedHeaders().IfNoneMatch.Any(match => match.Compare(etag, useStrongComparison: false)))
        {
            payload.GzippedContent.Dispose();
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        if (AcceptsGzip(request))
        {
            response.Headers.ContentEncoding = "gzip";
            return new FileStreamResult(payload.GzippedContent, "application/json");
        }

        return new FileStreamResult(new GZipStream(payload.GzippedContent, CompressionMode.Decompress), "application/json");
    }

    private static bool AcceptsGzip(HttpRequest request) =>
        request.GetTypedHeaders().AcceptEncoding.Any(encoding =>
            encoding.Value.Equals("gzip", StringComparison.OrdinalIgnoreCase) && encoding.Quality is not 0);
}
