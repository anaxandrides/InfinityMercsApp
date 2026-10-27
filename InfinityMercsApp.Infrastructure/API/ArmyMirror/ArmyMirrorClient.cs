using System.Net.Http.Headers;
using System.Text.Json;
using InfinityMercsApp.ApiClient;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;

namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>Reads the app's own copy of the army data: its manifest and the payloads, as Corvus Belli served them.</summary>
public sealed class ArmyMirrorClient(HttpClient httpClient, ArmyMirrorEndpoint endpoint) : IArmyMirror, IArmyPayloadSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<ArmyMirrorManifest?> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return JsonSerializer.Deserialize<ArmyMirrorManifest>(await GetAsync(endpoint.Manifest, cancellationToken), JsonOptions);
        }
        catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            Console.Error.WriteLine($"The army data copy's manifest couldn't be read: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DownloadLogoBundleAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(endpoint.Logos, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"The logo bundle answered {(int)response.StatusCode}.");
                return false;
            }

            await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var file = File.Create(filePath);
            await remote.CopyToAsync(file, cancellationToken);
            return true;
        }
        catch (Exception ex) when ((ex is HttpRequestException or IOException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            Console.Error.WriteLine($"The logo bundle couldn't be downloaded: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<string> GetMetadataAsync(CancellationToken cancellationToken = default) => GetAsync(endpoint.Metadata, cancellationToken);

    /// <summary>Which factions the Airborne, Inspiring and Cohesive companies list, as JSON (FactionListings).</summary>
    public Task<string> GetListingsAsync(CancellationToken cancellationToken = default) =>
        GetAsync(endpoint.Listings, cancellationToken);

    /// <summary>A generated company's faction, already built by the API (ArmyDefinitionJson).</summary>
    public Task<string> GetGeneratedArmyAsync(int factionId, CancellationToken cancellationToken = default) =>
        GetAsync(endpoint.Generated(factionId), cancellationToken);

    /// <inheritdoc/>
    public Task<string> GetArmyAsync(int factionId, CancellationToken cancellationToken = default) =>
        GetAsync(endpoint.Army(factionId), cancellationToken);

    private async Task<string> GetAsync(Uri url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{url} answered {(int)response.StatusCode}.", null, response.StatusCode);
        }

        return await HttpBodies.ReadAsync(response, cancellationToken);
    }
}
