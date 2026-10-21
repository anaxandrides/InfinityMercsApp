namespace InfinityMercsApp.Services.Sync;

/// <summary>Which version of a conflicting file the user keeps.</summary>
public enum SyncChoice
{
    /// <summary>This device's version replaces the other device's online.</summary>
    KeepThisDevice,

    /// <summary>The other device's version replaces this device's.</summary>
    UseOtherDevice
}
