namespace InfinityMercsApp.Services.Sync;

/// <summary>How a sync ended.</summary>
public enum SyncRunStatus
{
    Synced,

    /// <summary>Nobody is signed in, or the sign-in has to be renewed.</summary>
    SignedOut,

    /// <summary>The API couldn't be reached; the outbox is kept for next time.</summary>
    Offline
}
