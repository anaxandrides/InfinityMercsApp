using System.Text.Json;
using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>One of a user's files as Cosmos DB keeps it, in the user's own partition.</summary>
/// <param name="Id">The document id: the kind and the file's id, since a company and a season may share an id.</param>
/// <param name="UserId">The owner, which is also the partition key.</param>
public sealed record UserDocument(
    string Id,
    string UserId,
    UserDocumentKind Kind,
    string DocumentId,
    int Revision,
    DateTimeOffset UpdatedAt,
    bool Deleted,
    string? ArmySnapshotId,
    JsonElement? Content)
{
    public static string IdFor(UserDocumentKind kind, string documentId) => $"{kind.ToString().ToLowerInvariant()}-{documentId}";

    /// <param name="withContent">False for a listing, which leaves the files out.</param>
    public UserDocumentView ToView(bool withContent = true) => new()
    {
        DocumentId = DocumentId,
        Kind = Kind,
        Revision = Revision,
        UpdatedAt = UpdatedAt,
        Deleted = Deleted,
        ArmySnapshotId = ArmySnapshotId,
        Content = withContent ? Content : null
    };
}
