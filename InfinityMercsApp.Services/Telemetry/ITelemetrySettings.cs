namespace InfinityMercsApp.Services.Telemetry;

/// <summary>Whether the app sends telemetry, which the user can turn off in Settings.</summary>
public interface ITelemetrySettings
{
    bool IsEnabled { get; }

    /// <summary>Turns telemetry on or off; turning it off also throws away what is waiting to be sent.</summary>
    void SetEnabled(bool isEnabled);
}
