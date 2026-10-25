
namespace InfinityMercsApp.Web.Platform;

/// <summary>
/// Makes the <see cref="TelemetryLogger"/> each log category writes to. The telemetry store is looked up only when a
/// crash is reported: logging starts before the kept setting (the install id) is restored.
/// </summary>
public sealed class TelemetryLoggerProvider(IServiceProvider services) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new TelemetryLogger(categoryName, services);

    public void Dispose()
    {
    }
}
