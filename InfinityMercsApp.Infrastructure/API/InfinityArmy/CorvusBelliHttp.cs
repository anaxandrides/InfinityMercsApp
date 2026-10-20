using System.Net.Http.Headers;

namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

/// <summary>Where Corvus Belli's army API is, and how a request to it has to look.</summary>
public static class CorvusBelliHttp
{
    public const string ArmyUrlBase = "https://api.corvusbelli.com/army/units/en/";
    public const string MetadataUrl = "https://api.corvusbelli.com/army/infinity/en/metadata";

    /// <summary>The army payload's address for <paramref name="factionId"/>.</summary>
    public static string ArmyUrl(int factionId) => $"{ArmyUrlBase}{factionId}";

    /// <summary>A GET the API accepts.</summary>
    public static HttpRequestMessage NewRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        // The API refuses (403) any request with a User-Agent, which Android always sends, unless the Origin is
        // Corvus Belli's own site; the site moved from infinitytheuniverse.com in October 2026.
        request.Headers.TryAddWithoutValidation("Origin", "https://infinityuniverse.com");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        return request;
    }
}
