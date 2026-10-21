namespace InfinityMercsApp.UnitTests.Infrastructure.UserDocuments;

/// <summary>Fails every request as an unreachable server does.</summary>
internal sealed class FailingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No such host is known.");
}
