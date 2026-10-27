namespace InfinityMercsApp.Web.Platform;

/// <summary>
/// Asks the browser to finish small requests (telemetry) even if the tab closes meanwhile; browsers keep only requests
/// of up to 64 KB alive, so larger ones go as usual.
/// </summary>
public sealed class KeepAliveHandler : DelegatingHandler
{
    private const long MaxKeepAliveBytes = 60 * 1024;

    // How Blazor passes fetch options (WebAssemblyHttpRequestMessageExtensions.SetBrowserRequestOption).
    private static readonly HttpRequestOptionsKey<IDictionary<string, object>> FetchOptions = new("WebAssemblyFetchOptions");

    public KeepAliveHandler()
        : base(new HttpClientHandler())
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content?.Headers.ContentLength is <= MaxKeepAliveBytes)
        {
            if (!request.Options.TryGetValue(FetchOptions, out var options))
            {
                options = new Dictionary<string, object>(StringComparer.Ordinal);
                request.Options.Set(FetchOptions, options);
            }

            options["keepalive"] = true;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
