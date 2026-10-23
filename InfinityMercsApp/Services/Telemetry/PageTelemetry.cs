namespace InfinityMercsApp.Services.Telemetry;

/// <summary>Reports each page shown, with the Java object count, and how long the first page took after launch.</summary>
public sealed class PageTelemetry(ITelemetryRecorder recorder, TimeProvider time)
{
    public const string FirstPageMilestone = "FirstPage";

    private int _firstShown;

    /// <summary>The page the app shows now, as its type is named, e.g. "SettingsPage".</summary>
    public void Shown(string page)
    {
        var now = time.GetUtcNow();
        if (Interlocked.Exchange(ref _firstShown, 1) == 0 && LaunchClock.MillisecondsSinceLaunch() is { } milliseconds)
        {
            recorder.Record(TelemetryEvents.Startup(FirstPageMilestone, milliseconds, now));
        }

        recorder.Record(TelemetryEvents.PageShown(page, JavaObjects.Count(), now));
    }

    /// <summary>Reports <paramref name="page"/> through the app's <see cref="PageTelemetry"/>, for the page base classes.</summary>
    public static void Report(Page page) =>
        IPlatformApplication.Current?.Services.GetService<PageTelemetry>()?.Shown(page.GetType().Name);
}
