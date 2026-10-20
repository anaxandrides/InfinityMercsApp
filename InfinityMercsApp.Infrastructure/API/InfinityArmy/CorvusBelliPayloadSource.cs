namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

/// <inheritdoc/>
/// <remarks>Reads straight from Corvus Belli's army API.</remarks>
public sealed class CorvusBelliPayloadSource(HttpClient httpClient) : IArmyPayloadSource
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(80);

    /// <inheritdoc/>
    public Task<string> GetMetadataAsync(CancellationToken cancellationToken = default) =>
        GetAsync(CorvusBelliHttp.MetadataUrl, cancellationToken);

    /// <inheritdoc/>
    public Task<string> GetArmyAsync(int factionId, CancellationToken cancellationToken = default) =>
        GetAsync(CorvusBelliHttp.ArmyUrl(factionId), cancellationToken);

    private async Task<string> GetAsync(string url, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        using var request = CorvusBelliHttp.NewRequest(url);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{url} answered {(int)response.StatusCode}.", null, response.StatusCode);
        }

        return await CorvusBelliHttp.ReadBodyAsync(response, timeout.Token);
    }
}
