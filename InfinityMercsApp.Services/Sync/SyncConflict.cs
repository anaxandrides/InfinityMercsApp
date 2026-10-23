using System.Text.Json.Serialization;
using InfinityMercsApp.ApiClient.UserDocuments;

namespace InfinityMercsApp.Services.Sync;

/// <summary>
/// A file changed both here and on another device since they last synced. Nothing more is sent or fetched for it
/// until the user picks which version to keep.
/// </summary>
public sealed record SyncConflict
{
    public UserDocumentKind Kind { get; init; }

    public string DocumentId { get; init; } = string.Empty;

    /// <summary>The company's name, for a company or a season.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The revision online when the conflict was found.</summary>
    public int OtherRevision { get; init; }

    /// <summary>When the other device saved it.</summary>
    public DateTimeOffset OtherUpdatedAt { get; init; }

    /// <summary>Whether the other device deleted it.</summary>
    public bool OtherDeleted { get; init; }

    /// <summary>Whether this device deleted it.</summary>
    public bool DeletedHere { get; init; }

    [JsonIgnore]
    public DocumentKey Key => new(Kind, DocumentId);
}
