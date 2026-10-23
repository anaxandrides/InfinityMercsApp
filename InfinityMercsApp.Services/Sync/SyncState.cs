namespace InfinityMercsApp.Services.Sync;

/// <summary>The sync ledger as it is kept on disk.</summary>
public sealed record SyncState
{
    /// <summary>The account the revisions belong to; another account starts again from nothing.</summary>
    public string Account { get; init; } = string.Empty;

    public List<SyncedDocument> Documents { get; init; } = [];

    public List<SyncConflict> Conflicts { get; init; } = [];
}
