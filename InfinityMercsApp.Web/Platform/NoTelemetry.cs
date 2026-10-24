using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Services.Telemetry;

namespace InfinityMercsApp.Web.Platform;

/// <summary>The site doesn't report telemetry yet; what the shared services note goes nowhere.</summary>
public sealed class NoTelemetry : ITelemetryRecorder
{
    public void Record(TelemetryEvent telemetryEvent)
    {
    }
}
