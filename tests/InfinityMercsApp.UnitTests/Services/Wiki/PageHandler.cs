using System.Net;

namespace InfinityMercsApp.UnitTests.Services.Wiki;

/// <summary>Serves one HTML page, or fails, and counts the requests.</summary>
internal sealed class PageHandler(string? html) : HttpMessageHandler
{
    public int Requests { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests++;
        return html is null
            ? throw new HttpRequestException("The wiki is down.")
            : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) });
    }
}
