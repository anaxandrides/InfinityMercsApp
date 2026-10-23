using System.Text;
using InfinityMercsApp.ApiClient.Telemetry;

namespace InfinityMercsApp.Services.Telemetry;

/// <summary>The events the app reports, cut to what the API takes.</summary>
public static class TelemetryEvents
{
    /// <param name="javaObjects">How many Java objects the app holds (Android); null elsewhere.</param>
    public static TelemetryEvent PageShown(string page, int? javaObjects, DateTimeOffset at) => new()
    {
        Kind = TelemetryKind.PageShown,
        At = at,
        Name = Cut(page, TelemetryLimits.MaxNameLength),
        Value = javaObjects
    };

    /// <param name="milliseconds">From the app's launch until <paramref name="milestone"/>.</param>
    public static TelemetryEvent Startup(string milestone, double milliseconds, DateTimeOffset at) => new()
    {
        Kind = TelemetryKind.Startup,
        At = at,
        Name = Cut(milestone, TelemetryLimits.MaxNameLength),
        Value = Math.Max(0, milliseconds)
    };

    /// <param name="source">Where the crash was caught, e.g. "AppDomain.UnhandledException".</param>
    public static TelemetryEvent Crash(string source, Exception exception, DateTimeOffset at) => new()
    {
        Kind = TelemetryKind.Crash,
        At = at,
        Name = Cut(exception.GetType().FullName ?? exception.GetType().Name, TelemetryLimits.MaxNameLength),
        Detail = Cut($"{source}: {exception.Message}", TelemetryLimits.MaxDetailLength),
        StackTrace = Cut(StackTraceOf(exception), TelemetryLimits.MaxStackTraceLength)
    };

    public static TelemetryEvent SyncFailure(Exception exception, DateTimeOffset at) => new()
    {
        Kind = TelemetryKind.SyncFailure,
        At = at,
        Name = Cut(exception.GetType().FullName ?? exception.GetType().Name, TelemetryLimits.MaxNameLength),
        Detail = Cut(exception.Message, TelemetryLimits.MaxDetailLength)
    };

    // The exception's stack, then each inner exception's type, message and stack.
    private static string StackTraceOf(Exception exception)
    {
        var text = new StringBuilder(exception.StackTrace);
        IEnumerable<Exception> inners = exception switch
        {
            AggregateException aggregate => aggregate.InnerExceptions,
            { InnerException: { } inner } => [inner],
            _ => []
        };

        foreach (var innerException in inners)
        {
            text.AppendLine().Append("--- ").Append(innerException.GetType().FullName).Append(": ").AppendLine(innerException.Message)
                .Append(StackTraceOf(innerException));
        }

        return text.ToString();
    }

    private static string Cut(string text, int maxLength) => text.Length <= maxLength ? text : text[..maxLength];
}
