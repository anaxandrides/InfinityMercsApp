using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>Where users' files are kept, each user's apart from everyone else's.</summary>
public interface IUserDocumentStore
{
    /// <summary>The user's files of <paramref name="kind"/>, deleted ones included.</summary>
    Task<IReadOnlyList<UserDocument<TContent>>> ListAsync<TContent>(string userId, UserDocumentKind kind, CancellationToken cancellationToken = default)
        where TContent : class;

    /// <summary>The file, or null when the user has none with that id.</summary>
    Task<StoredUserDocument<TContent>?> ReadAsync<TContent>(string userId, UserDocumentKind kind, string documentId, CancellationToken cancellationToken = default)
        where TContent : class;

    /// <summary>Keeps a new file; false when one with its id already exists.</summary>
    Task<bool> TryCreateAsync<TContent>(UserDocument<TContent> document, CancellationToken cancellationToken = default)
        where TContent : class;

    /// <summary>Replaces a file; false when it changed since it was read as <paramref name="etag"/>.</summary>
    Task<bool> TryReplaceAsync<TContent>(UserDocument<TContent> document, string etag, CancellationToken cancellationToken = default)
        where TContent : class;
}
