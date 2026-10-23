namespace InfinityMercsApp.Services.Telemetry;

/// <summary>How long ago the app's process started.</summary>
public static class LaunchClock
{
    /// <summary>Milliseconds since the process started; null where the platform can't say.</summary>
    public static double? MillisecondsSinceLaunch()
    {
        try
        {
#if ANDROID
            return OperatingSystem.IsAndroidVersionAtLeast(24)
                ? Android.OS.SystemClock.ElapsedRealtime() - Android.OS.Process.StartElapsedRealtime
                : null;
#else
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            return (DateTime.Now - process.StartTime).TotalMilliseconds;
#endif
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or PlatformNotSupportedException)
        {
            return null;
        }
    }
}
