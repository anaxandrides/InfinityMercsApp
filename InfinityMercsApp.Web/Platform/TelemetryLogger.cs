using InfinityMercsApp.Services.Telemetry;

namespace InfinityMercsApp.Web.Platform;

/// <summary>Reports what Blazor logs as critical with an exception (an unhandled exception on a page) as a crash.</summary>
public sealed class TelemetryLogger(string category, IServiceProvider services) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Critical;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Critical && exception is not null)
        {
            var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
            services.GetRequiredService<ITelemetryRecorder>().Record(TelemetryEvents.Crash(category, exception, now));
        }
    }
}
