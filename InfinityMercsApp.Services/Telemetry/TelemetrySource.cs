namespace InfinityMercsApp.Services.Telemetry;

/// <summary>The app and device a telemetry batch comes from; never the device's own name, which is often a person's.</summary>
/// <param name="DeviceModel">Manufacturer and model, e.g. "Google Pixel 10".</param>
public sealed record TelemetrySource(string AppVersion, string Platform, string OsVersion, string DeviceModel);
