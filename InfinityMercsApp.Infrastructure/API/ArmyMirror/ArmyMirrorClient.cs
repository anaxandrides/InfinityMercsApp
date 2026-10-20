using System.Net.Http.Headers;
using System.Text.Json;
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
    public Task<string> GetMetadataAsync(CancellationToken cancellationToken = default) => GetAsync(endpoint.Metadata, cancellationToken);

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
