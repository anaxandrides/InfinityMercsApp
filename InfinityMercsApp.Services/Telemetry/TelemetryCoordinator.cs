namespace InfinityMercsApp.Services.Telemetry;

/// <inheritdoc/>
/// <remarks>Sends once the app has opened, so a crash's report goes out on the next launch, then every five minutes.</remarks>
internal sealed class TelemetryCoordinator(TelemetrySender sender) : ITelemetryCoordinator
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private int _started;

    /// <inheritdoc/>
    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 0)
        {
            _ = RunAsync();
        }
    }

    /// <inheritdoc/>
    public void SendSoon() => _ = sender.SendPendingAsync();

    private async Task RunAsync()
    {
        while (true)
        {
            await sender.SendPendingAsync();
            await Task.Delay(Interval);
        }
    }
}
