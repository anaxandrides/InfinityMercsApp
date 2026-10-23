using System.Text.Json.Serialization;
using InfinityMercsApp.ApiClient.UserDocuments;

namespace InfinityMercsApp.Services.Sync;

/// <summary>What this device knows of one of the user's files online.</summary>
public sealed record SyncedDocument
{
    public UserDocumentKind Kind { get; init; }

    public string DocumentId { get; init; } = string.Empty;

    /// <summary>The file, relative to the MercenaryRecords folder.</summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>The revision online that this device's copy builds on; 0 for a file never sent.</summary>
    public int Revision { get; init; }

    /// <summary>What is waiting to be sent.</summary>
    public SyncOperation Pending { get; init; }

    /// <summary>Whether the file is the signed-in account's, and so sent at all.</summary>
    public SyncOwnership Ownership { get; init; }

    /// <summary>Counts local changes, so a send only clears <see cref="Pending"/> when nothing changed meanwhile.</summary>
    public int Change { get; init; }

    [JsonIgnore]
    public DocumentKey Key => new(Kind, DocumentId);
}
