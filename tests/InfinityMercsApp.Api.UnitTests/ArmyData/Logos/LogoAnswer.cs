using System.Net;

namespace InfinityMercsApp.Api.UnitTests.ArmyData.Logos;

/// <summary>Answers with a two-byte logo and the status, or fails as an unreachable CDN does when there is none.</summary>
internal sealed class LogoAnswer(HttpStatusCode? status) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        status is { } code
            ? Task.FromResult(new HttpResponseMessage(code) { Content = new ByteArrayContent([1, 2]) })
            : throw new HttpRequestException("No route to host.");
}
