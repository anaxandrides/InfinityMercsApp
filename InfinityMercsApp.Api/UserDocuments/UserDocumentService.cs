using InfinityMercsApp.ApiClient.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>
/// A user's companies and seasons. Every save says which revision it builds on, so a save from a device that missed
/// another device's save is refused instead of overwriting it.
/// </summary>
public sealed class UserDocumentService(IUserDocumentStore store, TimeProvider time)
{
    /// <summary>The user's files of <paramref name="kind"/>, without their content, deleted ones included.</summary>
    public async Task<IReadOnlyList<UserDocumentView<TContent>>> ListAsync<TContent>(
        string userId,
        UserDocumentKind kind,
        CancellationToken cancellationToken = default)
        where TContent : class =>
        (await store.ListAsync<TContent>(userId, kind, cancellationToken))
            .OrderBy(document => document.DocumentId, StringComparer.Ordinal)
            .Select(document => document.ToView(withContent: false))
            .ToList();

    /// <summary>The file, or null when the user has none with that id.</summary>
    public async Task<UserDocumentView<TContent>?> GetAsync<TContent>(
        string userId,
        UserDocumentKind kind,
        string documentId,
        CancellationToken cancellationToken = default)
        where TContent : class =>
        (await store.ReadAsync<TContent>(userId, kind, documentId, cancellationToken))?.Document.ToView();

    /// <summary>Saves the file when <see cref="UserDocumentWrite{TContent}.BaseRevision"/> is its current revision (0 for a new one).</summary>
    public Task<SaveOutcome<TContent>> SaveAsync<TContent>(
        string userId,
        UserDocumentKind kind,
        string documentId,
        UserDocumentWrite<TContent> write,
        CancellationToken cancellationToken = default)
        where TContent : class =>
        WriteAsync(userId, kind, documentId, write.BaseRevision, write.ArmySnapshotId, write.Content, deleted: false, cancellationToken);

    /// <summary>
    /// Deletes the file when <paramref name="baseRevision"/> is its current revision. It is kept as a new, deleted
    /// revision, so the user's other devices learn of the deletion; its content is kept too, so a deletion can be
    /// undone, but the API no longer returns it.
    /// </summary>
    public async Task<SaveOutcome<TContent>> DeleteAsync<TContent>(
        string userId,
        UserDocumentKind kind,
        string documentId,
        int baseRevision,
        CancellationToken cancellationToken = default)
        where TContent : class
    {
        if (await store.ReadAsync<TContent>(userId, kind, documentId, cancellationToken) is not { Document: { Deleted: false } current })
        {
            return SaveOutcome<TContent>.NotFound;
        }

        return await WriteAsync(userId, kind, documentId, baseRevision, current.ArmySnapshotId, current.Content, deleted: true, cancellationToken);
    }

    private async Task<SaveOutcome<TContent>> WriteAsync<TContent>(
        string userId,
        UserDocumentKind kind,
        string documentId,
        int baseRevision,
        string? armySnapshotId,
        TContent? content,
        bool deleted,
        CancellationToken cancellationToken)
        where TContent : class
    {
        var stored = await store.ReadAsync<TContent>(userId, kind, documentId, cancellationToken);
        var currentRevision = stored?.Document.Revision ?? 0;
        if (baseRevision != currentRevision)
        {
            return Conflict(stored);
        }

        var document = new UserDocument<TContent>(
            UserDocumentIds.For(kind, documentId),
            userId,
            kind,
            documentId,
            currentRevision + 1,
            time.GetUtcNow(),
            Deleted: deleted,
            armySnapshotId,
            content);

        var written = stored is null
            ? await store.TryCreateAsync(document, cancellationToken)
            : await store.TryReplaceAsync(document, stored.ETag, cancellationToken);
        return written
            ? new SaveOutcome<TContent>(SaveResult.Saved, document.ToView())
            : Conflict(await store.ReadAsync<TContent>(userId, kind, documentId, cancellationToken));
    }

    private static SaveOutcome<TContent> Conflict<TContent>(StoredUserDocument<TContent>? current)
        where TContent : class =>
        new(SaveResult.Conflict, current?.Document.ToView());
}
