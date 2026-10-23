namespace InfinityMercsApp.Services.Telemetry;

/// <summary>Sends the telemetry the app has noted while it is open.</summary>
public interface ITelemetryCoordinator
{
    /// <summary>Starts sending: now, which sends what earlier sessions left, and then every few minutes.</summary>
    void Start();

    /// <summary>Sends what is waiting without waiting for it to go, as when the app goes to the background.</summary>
    void SendSoon();
}
