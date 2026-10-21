using System.IO.Compression;

namespace InfinityMercsApp.ApiClient;

/// <summary>Reads an answer's body, which the army APIs send gzipped.</summary>
public static class HttpBodies
{
    /// <summary>The body as text, unzipped when it came gzipped.</summary>
    public static async Task<string> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if (response.Content.Headers.ContentEncoding.Any(encoding => encoding.Equals("gzip", StringComparison.OrdinalIgnoreCase)))
        {
            await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        using var plainReader = new StreamReader(stream);
        return await plainReader.ReadToEndAsync(cancellationToken);
    }
}
