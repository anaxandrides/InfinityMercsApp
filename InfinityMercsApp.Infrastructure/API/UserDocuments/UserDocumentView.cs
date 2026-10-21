namespace InfinityMercsApp.Infrastructure.API.UserDocuments;

/// <summary>One of the user's files, a company or a season, as the API keeps it.</summary>
public sealed record UserDocumentView<TContent>
    where TContent : class
{
    /// <summary>The file's id, chosen by the app that created it.</summary>
    public string DocumentId { get; init; } = string.Empty;

    /// <summary>How many times the file was saved; each save adds one.</summary>
    public int Revision { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Whether the file was deleted; it is kept, empty, so other devices learn of the deletion.</summary>
    public bool Deleted { get; init; }

    public string? ArmySnapshotId { get; init; }

    /// <summary>The file; null when deleted, or in a listing.</summary>
    public TContent? Content { get; init; }
}
