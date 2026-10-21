using System.Net;

namespace InfinityMercsApp.UnitTests.InfinityArmy;

/// <summary>Answers every request with one canned response and keeps the request it got.</summary>
internal sealed class RecordingHandler(HttpStatusCode status, HttpContent content) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        return Task.FromResult(new HttpResponseMessage(status) { Content = content });
    }
}
