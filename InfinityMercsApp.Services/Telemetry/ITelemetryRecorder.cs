using InfinityMercsApp.ApiClient.Telemetry;

namespace InfinityMercsApp.Services.Telemetry;

/// <summary>Notes what the app reports, to be sent later; does nothing while telemetry is off, and never throws.</summary>
public interface ITelemetryRecorder
{
    /// <summary>Writes the event to this session's file straight away, so a crash's report survives the crash.</summary>
    void Record(TelemetryEvent telemetryEvent);
}
