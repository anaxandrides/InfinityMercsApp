namespace InfinityMercsApp.Services.Feedback;

/// <summary>The app version and device a feedback report came from.</summary>
/// <param name="Model">Manufacturer and model, e.g. "Google Pixel 8".</param>
public sealed record DeviceDetails(string AppVersion, string Platform, string Model, string Name);
