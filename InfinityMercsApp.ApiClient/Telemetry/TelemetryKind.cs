using System.Text.Json.Serialization;

namespace InfinityMercsApp.ApiClient.Telemetry;

/// <summary>What a telemetry event reports.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TelemetryKind>))]
public enum TelemetryKind
{
    /// <summary>A page was shown: its name, and on Android how many Java objects the app holds then.</summary>
    PageShown,

    /// <summary>The app crashed: the exception's type as the name, its message as the detail, and its stack trace.</summary>
    Crash,

    /// <summary>The app opened: milliseconds from launch until its first page.</summary>
    Startup,

    /// <summary>A sync failed with an error: the error's type as the name, its message as the detail.</summary>
    SyncFailure
}
