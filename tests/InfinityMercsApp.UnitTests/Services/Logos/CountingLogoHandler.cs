using System.Net;

namespace InfinityMercsApp.UnitTests.Services.Logos;

/// <summary>Serves a small SVG for every request after a short wait, counting requests and how many ran at once.</summary>
internal sealed class CountingLogoHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    private int _running;
    private int _requests;
    private int _mostAtOnce;

    public int Requests => _requests;

    public int MostAtOnce => _mostAtOnce;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requests);
        var running = Interlocked.Increment(ref _running);
        int seen;
        while ((seen = _mostAtOnce) < running && Interlocked.CompareExchange(ref _mostAtOnce, running, seen) != seen)
        {
        }

        await Task.Delay(20, cancellationToken);
        Interlocked.Decrement(ref _running);
        return new HttpResponseMessage(status) { Content = new StringContent("<svg/>") };
    }
}
