namespace InfinityMercsApp.Services.Sync;

/// <summary>What a sync did, and the conflicts waiting for the user.</summary>
/// <param name="Unclaimed">Files on this device that aren't in the account, waiting for the user to add them.</param>
public sealed record SyncReport(SyncRunStatus Status, int Sent, int Received, IReadOnlyList<SyncConflict> Conflicts, UnclaimedFiles Unclaimed)
{
    public static SyncReport SignedOut { get; } = new(SyncRunStatus.SignedOut, 0, 0, [], UnclaimedFiles.None);

    /// <summary>One line for the settings page.</summary>
    public string Summary => Status switch
    {
        SyncRunStatus.SignedOut => "Sign in to keep your companies and seasons online.",
        SyncRunStatus.Offline => "Couldn't reach the server; changes will be sent later.",
        _ when Conflicts.Count > 0 => $"Synced. {Conflicts.Count} {(Conflicts.Count == 1 ? "file was" : "files were")} changed on another device too.",
        _ when Unclaimed.Total > 0 => $"Synced. {Unclaimed.Total} {(Unclaimed.Total == 1 ? "file" : "files")} on this device {(Unclaimed.Total == 1 ? "isn't" : "aren't")} in your account yet.",
        _ => $"Synced: {Sent} sent, {Received} received."
    };
}
