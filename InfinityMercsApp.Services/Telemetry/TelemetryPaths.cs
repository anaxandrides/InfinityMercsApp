namespace InfinityMercsApp.Services.Telemetry;

/// <summary>Where the app keeps telemetry waiting to be sent, and the telemetry setting.</summary>
/// <param name="Directory">The Telemetry folder in the app's data.</param>
public sealed record TelemetryPaths(string Directory)
{
    /// <summary>One JSON-lines file per session, waiting to be sent.</summary>
    public string OutboxDirectory => Path.Combine(Directory, "Outbox");

    /// <summary>The install id and whether telemetry is on.</summary>
    public string SettingsPath => Path.Combine(Directory, "telemetry-settings.json");

    public static TelemetryPaths ForAppData(string appDataDirectory) => new(Path.Combine(appDataDirectory, "Telemetry"));
}
