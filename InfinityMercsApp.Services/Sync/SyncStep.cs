namespace InfinityMercsApp.Services.Sync;

/// <summary>How sending or fetching one file went.</summary>
internal enum SyncStep
{
    /// <summary>Nothing moved: already the same, held for the user, or refused.</summary>
    Unchanged,

    /// <summary>The file was sent or fetched.</summary>
    Moved,

    /// <summary>The API couldn't be reached; the rest waits for next time.</summary>
    Offline,

    /// <summary>The sign-in wasn't accepted; the rest waits for the next sign-in.</summary>
    SignedOut
}
