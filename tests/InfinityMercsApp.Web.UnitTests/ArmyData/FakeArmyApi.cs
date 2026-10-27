using System.Net;
using System.Text;

namespace InfinityMercsApp.Web.UnitTests.ArmyData;

/// <summary>Answers the army data routes from the bodies given, and counts what was asked for.</summary>
internal sealed class FakeArmyApi(IReadOnlyDictionary<string, string> bodies) : HttpMessageHandler
{
    public List<string> Requested { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath.Replace("/api/", string.Empty, StringComparison.Ordinal) ?? string.Empty;
        Requested.Add(path);
        return Task.FromResult(bodies.TryGetValue(path, out var body)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
