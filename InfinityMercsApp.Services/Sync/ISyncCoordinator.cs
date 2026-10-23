namespace InfinityMercsApp.Services.Sync;

/// <summary>Runs sync in the background while the app is open, and asks the user about conflicts.</summary>
public interface ISyncCoordinator
{
    /// <summary>Starts syncing: now, a few seconds after each save, and every so often.</summary>
    void Start();

    /// <summary>Syncs straight away, asking again about conflicts the user put off.</summary>
    Task<SyncReport> SyncNowAsync();
}
