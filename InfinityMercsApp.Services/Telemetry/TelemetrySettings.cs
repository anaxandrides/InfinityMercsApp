namespace InfinityMercsApp.Services.Telemetry;

/// <summary>The telemetry setting as it is saved.</summary>
/// <param name="InstallId">A random id made on the first launch, so one install's reports can be followed; not tied to a person.</param>
/// <param name="IsEnabled">Whether the app reports anything; on until the user turns it off.</param>
public sealed record TelemetrySettings(string InstallId, bool IsEnabled)
{
    /// <summary>The settings of an install that has none yet: a new id, with telemetry on.</summary>
    public static TelemetrySettings New() => new(Guid.NewGuid().ToString("D"), IsEnabled: true);
}
