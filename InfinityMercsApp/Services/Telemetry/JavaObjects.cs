namespace InfinityMercsApp.Services.Telemetry;

/// <summary>
/// How many Java objects the app holds a reference to. Android hangs and then crashes once there are tens of
/// thousands, so the count is reported with each page shown.
/// </summary>
public static class JavaObjects
{
    /// <summary>The count on Android; null elsewhere.</summary>
    public static int? Count()
    {
#if ANDROID
        return Java.Interop.JniRuntime.CurrentRuntime.GlobalReferenceCount;
#else
        return null;
#endif
    }
}
