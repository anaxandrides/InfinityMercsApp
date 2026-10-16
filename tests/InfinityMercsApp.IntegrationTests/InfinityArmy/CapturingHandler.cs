using System.IO.Compression;

namespace InfinityMercsApp.IntegrationTests.InfinityArmy;

/// <summary>
/// Passes requests through to the network unchanged and keeps each response body, decompressed, so a test can
/// check the raw JSON the API client was given.
/// </summary>
internal sealed class CapturingHandler() : DelegatingHandler(new HttpClientHandler())
{
    private readonly Dictionary<string, string> _bodies = new(StringComparer.Ordinal);

    /// <summary>The body last received from the URL.</summary>
    public string BodyFrom(string url) =>
        _bodies.TryGetValue(url, out var body) ? body : throw new InvalidOperationException($"Nothing was received from {url}.");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // Buffered content can be read again by the API client after it is read here; the body is read from a
        // copy so the client's own stream stays open.
        await response.Content.LoadIntoBufferAsync(cancellationToken);
        using var stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync(cancellationToken));
        var gzipped = response.Content.Headers.ContentEncoding.Any(encoding => encoding.Equals("gzip", StringComparison.OrdinalIgnoreCase));
        using var reader = new StreamReader(gzipped ? new GZipStream(stream, CompressionMode.Decompress) : stream);
        _bodies[request.RequestUri?.ToString() ?? string.Empty] = await reader.ReadToEndAsync(cancellationToken);

        return response;
    }
}
