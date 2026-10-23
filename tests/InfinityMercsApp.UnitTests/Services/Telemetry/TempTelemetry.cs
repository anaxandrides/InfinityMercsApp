using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Services.Telemetry;

namespace InfinityMercsApp.UnitTests.Services.Telemetry;

/// <summary>A throwaway telemetry folder.</summary>
internal sealed class TempTelemetry : IDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    public TelemetryPaths Paths { get; } = new(Path.Combine(Path.GetTempPath(), "mercs-telemetry-tests-" + Guid.NewGuid().ToString("N")));

    public TelemetryStore NewStore() => new(Paths, TimeProvider.System);

    public static TelemetryEvent Page(string name) => TelemetryEvents.PageShown(name, javaObjects: 100, Now);

    public void Dispose()
    {
        if (Directory.Exists(Paths.Directory))
        {
            Directory.Delete(Paths.Directory, recursive: true);
        }
    }
}
