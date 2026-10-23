namespace InfinityMercsApp.Services.Sync;

/// <summary>Whose a file on this device is, which decides whether it is sent online.</summary>
public enum SyncOwnership
{
    /// <summary>The signed-in account's: sent as it changes.</summary>
    Claimed,

    /// <summary>
    /// Made while signed out, found on the device, or left by another account: not sent until the user adds it to
    /// their account.
    /// </summary>
    Unclaimed,

    /// <summary>Kept on this device at the user's choice: never sent.</summary>
    DeviceOnly
}
