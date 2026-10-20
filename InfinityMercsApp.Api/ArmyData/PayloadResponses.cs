using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>How a mirrored payload goes back to a client: gzipped when it takes gzip, and not at all when its copy is current.</summary>
public static class PayloadResponses
{
    /// <summary>Clients may reuse a payload for an hour before asking again; asking again is cheap with the ETag.</summary>
    public const string CacheControl = "public, max-age=3600";

    public static IActionResult For(HttpRequest request, StoredPayload? payload)
    {
        if (payload is null)
        {
            return new NotFoundResult();
        }

        var etag = new EntityTagHeaderValue($"\"{payload.Hash}\"");
        var response = request.HttpContext.Response;
        response.Headers.ETag = etag.ToString();
        response.Headers.CacheControl = CacheControl;
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
