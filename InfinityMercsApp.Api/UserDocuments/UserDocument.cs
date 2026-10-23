using InfinityMercsApp.ApiClient.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>One of a user's files as Cosmos DB keeps it, in the user's own partition.</summary>
/// <param name="Id">The document id: the kind and the file's id, since a company and a season may share an id.</param>
/// <param name="UserId">The owner, which is also the partition key.</param>
/// <param name="Content">The file; still kept once deleted, so a deletion can be undone, but not returned by the API.</param>
public sealed record UserDocument<TContent>(
    string Id,
    string UserId,
    UserDocumentKind Kind,
    string DocumentId,
    int Revision,
    DateTimeOffset UpdatedAt,
    bool Deleted,
    string? ArmySnapshotId,
    TContent? Content)
    where TContent : class
{
    /// <param name="withContent">False for a listing, which leaves the files out. A deleted file's content is never returned.</param>
    public UserDocumentView<TContent> ToView(bool withContent = true) => new()
    {
        DocumentId = DocumentId,
        Revision = Revision,
        UpdatedAt = UpdatedAt,
        Deleted = Deleted,
        ArmySnapshotId = ArmySnapshotId,
        Content = withContent && !Deleted ? Content : null
    };
}
