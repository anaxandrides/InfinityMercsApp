using System.Net;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>The site's package folder: a few packaged files, their list, and one that fails.</summary>
internal sealed class PackageServer : HttpMessageHandler
{
    private static readonly Dictionary<string, string> Files = new(StringComparer.Ordinal)
    {
        ["/package/files.txt"] = "SVGCache/units/fusilier.svg\r\n\nGlossary/glossary.json\n",
        ["/package/SVGCache/units/fusilier.svg"] = "<svg/>",
        ["/package/Glossary/glossary.json"] = "glossary"
    };

    public int ListRequests { get; private set; }

    public List<string> Requested { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        Requested.Add(request.RequestUri?.ToString() ?? string.Empty);
        ListRequests += path == "/package/files.txt" ? 1 : 0;
        var response = Files.TryGetValue(path, out var body)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }
            : new HttpResponseMessage(path == "/package/broken.json" ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound);
        return Task.FromResult(response);
    }
}
