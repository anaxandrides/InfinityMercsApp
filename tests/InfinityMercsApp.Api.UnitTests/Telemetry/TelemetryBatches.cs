using InfinityMercsApp.ApiClient.Telemetry;

namespace InfinityMercsApp.Api.UnitTests.Telemetry;

/// <summary>Batches for the telemetry tests, sent at <see cref="Now"/>.</summary>
internal static class TelemetryBatches
{
    public static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    public static TelemetryEvent PageShown { get; } = new() { Kind = TelemetryKind.PageShown, At = Now, Name = "MercsSeasonPage", Value = 12345 };

    public static TelemetryEvent Crash { get; } = new()
    {
        Kind = TelemetryKind.Crash,
        At = Now,
        Name = "System.InvalidOperationException",
        Detail = "Sequence contains no elements",
        StackTrace = "   at Foo.Bar()"
    };

    public static TelemetryBatch With(params IEnumerable<TelemetryEvent> events) => new()
    {
        InstallId = "6f9619ff-8b86-d011-b42d-00c04fc964ff",
        AppVersion = "0.1.0",
        Platform = "Android",
        OsVersion = "16",
        DeviceModel = "Google Pixel 10",
        Events = events.ToList()
    };
}
