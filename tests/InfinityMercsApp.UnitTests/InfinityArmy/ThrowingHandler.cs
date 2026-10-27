namespace InfinityMercsApp.UnitTests.InfinityArmy;

/// <summary>Fails every request as an unreachable server does.</summary>
internal sealed class ThrowingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No route to host.");
}
