using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InfinityMercsApp.ApiClient.Telemetry;

/// <inheritdoc/>
public sealed class TelemetryApiClient(HttpClient httpClient, TelemetryEndpoint endpoint) : ITelemetryApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<TelemetrySendResult> SendAsync(TelemetryBatch batch, string? accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Batches)
        {
            Content = JsonContent.Create(batch, options: JsonOptions)
        };
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return ResultOf(response.StatusCode);
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            return TelemetrySendResult.Unavailable;
        }
    }

    private static TelemetrySendResult ResultOf(HttpStatusCode status) => status switch
    {
        >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous => TelemetrySendResult.Delivered,
        HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge => TelemetrySendResult.Refused,
        _ => TelemetrySendResult.Unavailable
    };
}
