namespace InfinityMercsApp.ApiClient.UserDocuments;

/// <summary>
/// The signed-in user's companies and seasons online. Every call takes the access token, and never throws for a
/// failed request: the reply says how it went.
/// </summary>
public interface IUserDocumentsApi
{
    /// <summary>Every file of the kind, deleted ones included, without their content.</summary>
    Task<UserDocumentsReply<IReadOnlyList<UserDocumentView<TContent>>>> ListAsync<TContent>(
        UserDocumentKind kind, string accessToken, CancellationToken cancellationToken = default)
        where TContent : class;

    Task<UserDocumentsReply<UserDocumentView<TContent>>> GetAsync<TContent>(
        UserDocumentKind kind, string documentId, string accessToken, CancellationToken cancellationToken = default)
        where TContent : class;

    /// <summary>Saves the file over the revision it builds on; a conflict carries the file as it now is.</summary>
    Task<UserDocumentsReply<UserDocumentView<TContent>>> SaveAsync<TContent>(
        UserDocumentKind kind, string documentId, UserDocumentWrite<TContent> write, string accessToken, CancellationToken cancellationToken = default)
        where TContent : class;

    /// <summary>Deletes the file when <paramref name="baseRevision"/> is still its revision; a conflict carries the file as it now is.</summary>
    Task<UserDocumentsReply<UserDocumentView<TContent>>> DeleteAsync<TContent>(
        UserDocumentKind kind, string documentId, int baseRevision, string accessToken, CancellationToken cancellationToken = default)
        where TContent : class;
}
