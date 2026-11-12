namespace InfinityMercsApp.ApiClient.Telemetry;

/// <summary>One thing the app reports. Never personal: page names, counts, timings and error details only.</summary>
public sealed record TelemetryEvent
{
    public TelemetryKind Kind { get; init; }

    public DateTimeOffset At { get; init; }

    /// <summary>The page, or the exception's type.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The Java object count, or the milliseconds; null when the event has no number.</summary>
    public double? Value { get; init; }

    /// <summary>An error's message; null otherwise.</summary>
    public string? Detail { get; init; }

    /// <summary>A crash's stack trace, inner exceptions included; null otherwise.</summary>
    public string? StackTrace { get; init; }
}
