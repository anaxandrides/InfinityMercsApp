namespace InfinityMercsApp.ApiClient.UserDocuments;

/// <summary>A save of one of the user's files: a company or a season.</summary>
public sealed record UserDocumentWrite<TContent>
    where TContent : class
{
    /// <summary>
    /// The revision the client last read, which the save builds on: 0 for a new file. When another device saved since,
    /// the save is refused and the client gets the current file to merge with.
    /// </summary>
    public int BaseRevision { get; init; }

    /// <summary>The army data snapshot the file was built with, if it pins one.</summary>
    public string? ArmySnapshotId { get; init; }

    /// <summary>The file itself.</summary>
    public required TContent Content { get; init; }
}
