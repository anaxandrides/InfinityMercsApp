namespace InfinityMercsApp.ApiClient.Telemetry;

/// <summary>A batch of one install's events, as POST telemetry takes them.</summary>
public sealed record TelemetryBatch
{
    /// <summary>A random id the app made on its first launch, so one install's reports can be followed; not tied to a person.</summary>
    public string InstallId { get; init; } = string.Empty;

    public string AppVersion { get; init; } = string.Empty;

    /// <summary>"Android", "WinUI", "iOS" and so on.</summary>
    public string Platform { get; init; } = string.Empty;

    public string OsVersion { get; init; } = string.Empty;

    /// <summary>The device's make and model, e.g. "Google Pixel 10".</summary>
    public string DeviceModel { get; init; } = string.Empty;

    public IReadOnlyList<TelemetryEvent> Events { get; init; } = [];
}
