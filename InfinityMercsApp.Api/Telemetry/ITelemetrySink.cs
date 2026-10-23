using InfinityMercsApp.ApiClient.Telemetry;

namespace InfinityMercsApp.Api.Telemetry;

/// <summary>Where an app's telemetry is kept once it passes the checks.</summary>
public interface ITelemetrySink
{
    /// <param name="userId">The signed-in user the batch came from; null when it came without a valid token.</param>
    void Write(TelemetryBatch batch, string? userId);
}
