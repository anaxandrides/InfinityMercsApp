namespace InfinityMercsApp.Services.Sync;

/// <summary>What the user does with files on this device that aren't in their account.</summary>
public enum ClaimChoice
{
    /// <summary>Adds them to the account; they are sent at the next sync.</summary>
    AddToAccount,

    /// <summary>Keeps them on this device only; they are never sent.</summary>
    KeepOnDevice
}
