using System.Net;
using System.Text;

namespace InfinityMercsApp.AotSmokeTests.Support;

/// <summary>Answers every request with one JSON body, keeping the last body it was sent.</summary>
public sealed class CannedHandler(string reply) : HttpMessageHandler
{
    public string? Sent { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Sent = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(reply, Encoding.UTF8, "application/json") };
    }
}
