using Microsoft.Extensions.Logging;

namespace InfinityMercsApp.Api.ArmyData.Logos;

/// <inheritdoc/>
/// <remarks>Downloads straight from the address the army data gives, Corvus Belli's CDN.</remarks>
public sealed class HttpLogoSource(HttpClient httpClient, ILogger<HttpLogoSource> logger) : ILogoSource
{
    /// <inheritdoc/>
    public async Task<byte[]?> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }

            logger.LogInformation("Logo {Url} answered {Status}.", url, (int)response.StatusCode);
            return null;
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Logo {Url} couldn't be downloaded: {Message}", url, ex.Message);
            return null;
        }
    }
}
