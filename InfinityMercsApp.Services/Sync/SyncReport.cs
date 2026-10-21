namespace InfinityMercsApp.Services.Sync;

/// <summary>What a sync did, and the conflicts waiting for the user.</summary>
public sealed record SyncReport(SyncRunStatus Status, int Sent, int Received, IReadOnlyList<SyncConflict> Conflicts)
{
    public static SyncReport SignedOut { get; } = new(SyncRunStatus.SignedOut, 0, 0, []);

    /// <summary>One line for the settings page.</summary>
    public string Summary => Status switch
    {
        SyncRunStatus.SignedOut => "Sign in to keep your companies and seasons online.",
        SyncRunStatus.Offline => "Couldn't reach the server; changes will be sent later.",
        _ when Conflicts.Count > 0 => $"Synced. {Conflicts.Count} {(Conflicts.Count == 1 ? "file was" : "files were")} changed on another device too.",
        _ => $"Synced: {Sent} sent, {Received} received."
    };
}
