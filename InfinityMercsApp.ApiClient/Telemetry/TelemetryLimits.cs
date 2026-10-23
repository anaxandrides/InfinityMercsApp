namespace InfinityMercsApp.ApiClient.Telemetry;

/// <summary>What the API accepts in a batch; the app keeps within it.</summary>
public static class TelemetryLimits
{
    public const int MaxEvents = 200;
    public const int MaxNameLength = 120;
    public const int MaxDetailLength = 4000;
    public const int MaxStackTraceLength = 16000;
    public const int MaxFieldLength = 64;

    /// <summary>The largest body the API reads.</summary>
    public const int MaxBodyBytes = 1_000_000;
}
