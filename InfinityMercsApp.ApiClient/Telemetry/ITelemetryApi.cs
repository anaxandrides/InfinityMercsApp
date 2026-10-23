namespace InfinityMercsApp.ApiClient.Telemetry;

/// <summary>Sends telemetry batches to the app's own API, which keeps them in Application Insights.</summary>
public interface ITelemetryApi
{
    /// <summary>Sends the batch, with the sign-in token when there is one so the API can note whose it is.</summary>
    Task<TelemetrySendResult> SendAsync(TelemetryBatch batch, string? accessToken, CancellationToken cancellationToken = default);
}
