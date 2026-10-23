using Microsoft.AspNetCore.Http;

namespace InfinityMercsApp.Api.Http;

/// <summary>Reads a request's body without taking more than a route allows.</summary>
public static class RequestBodies
{
    /// <summary>The body, or null when it is larger than <paramref name="maxBytes"/>.</summary>
    public static async Task<byte[]?> ReadAsync(HttpRequest request, int maxBytes, CancellationToken cancellationToken)
    {
        if (request.ContentLength > maxBytes)
        {
            return null;
        }

        using var body = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (body.Length + read > maxBytes)
            {
                return null;
            }

            body.Write(buffer, 0, read);
        }

        return body.ToArray();
    }
}
