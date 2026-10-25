using System.Net;
using System.Text;

namespace InfinityMercsApp.UnitTests.Services.Feedback;

/// <summary>Answers the feedback form with one canned reply, keeping the body it was sent.</summary>
internal sealed class FormHandler(HttpStatusCode status, string reply, string mediaType = "application/json") : HttpMessageHandler
{
    public string? SentBody { get; private set; }

    public Uri? SentTo { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentTo = request.RequestUri;
        SentBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(status) { ReasonPhrase = status.ToString(), Content = new StringContent(reply, Encoding.UTF8, mediaType) };
    }
}
