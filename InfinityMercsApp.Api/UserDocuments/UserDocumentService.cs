using System.Text.Json;
using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>
/// A user's companies and seasons. Every save says which revision it builds on, so a save from a device that missed
/// another device's save is refused instead of overwriting it.
/// </summary>
public sealed class UserDocumentService(IUserDocumentStore store, TimeProvider time)
{
    /// <summary>The user's files of <paramref name="kind"/>, without their content, deleted ones included.</summary>
    public async Task<IReadOnlyList<UserDocumentView>> ListAsync(string userId, UserDocumentKind kind, CancellationToken cancellationToken = default) =>
        (await store.ListAsync(userId, kind, cancellationToken))
            .OrderBy(document => document.DocumentId, StringComparer.Ordinal)
            .Select(document => document.ToView(withContent: false))
            .ToList();

    /// <summary>The file, or null when the user has none with that id.</summary>
    public async Task<UserDocumentView?> GetAsync(string userId, UserDocumentKind kind, string documentId, CancellationToken cancellationToken = default) =>
        (await store.ReadAsync(userId, kind, documentId, cancellationToken))?.Document.ToView();

    /// <summary>Saves the file when <see cref="UserDocumentWrite.BaseRevision"/> is its current revision (0 for a new one).</summary>
    public Task<SaveOutcome> SaveAsync(
        string userId,
        UserDocumentKind kind,
        string documentId,
        UserDocumentWrite write,
        CancellationToken cancellationToken = default) =>
        WriteAsync(userId, kind, documentId, write.BaseRevision, write.ArmySnapshotId, write.Content, cancellationToken);

    /// <summary>
    /// Deletes the file when <paramref name="baseRevision"/> is its current revision. It is kept, empty, as a new
    /// revision, so the user's other devices learn of the deletion.
    /// </summary>
    public async Task<SaveOutcome> DeleteAsync(
        string userId,
        UserDocumentKind kind,
        string documentId,
        int baseRevision,
        CancellationToken cancellationToken = default)
    {
        if (await store.ReadAsync(userId, kind, documentId, cancellationToken) is not { Document.Deleted: false })
        {
            return SaveOutcome.NotFound;
        }

        return await WriteAsync(userId, kind, documentId, baseRevision, armySnapshotId: null, content: null, cancellationToken);
    }

    private async Task<SaveOutcome> WriteAsync(
        string userId,
        UserDocumentKind kind,
        string documentId,
        int baseRevision,
        string? armySnapshotId,
        JsonElement? content,
        CancellationToken cancellationToken)
    {
        var stored = await store.ReadAsync(userId, kind, documentId, cancellationToken);
        var currentRevision = stored?.Document.Revision ?? 0;
        if (baseRevision != currentRevision)
        {
            return Conflict(stored);
        }

        var document = new UserDocument(
            UserDocument.IdFor(kind, documentId),
            userId,
            kind,
            documentId,
            currentRevision + 1,
            time.GetUtcNow(),
            Deleted: content is null,
            armySnapshotId,
            content);

        var written = stored is null
            ? await store.TryCreateAsync(document, cancellationToken)
            : await store.TryReplaceAsync(document, stored.ETag, cancellationToken);
        return written
            ? new SaveOutcome(SaveResult.Saved, document.ToView())
            : Conflict(await store.ReadAsync(userId, kind, documentId, cancellationToken));
    }

    private static SaveOutcome Conflict(StoredUserDocument? current) => new(SaveResult.Conflict, current?.Document.ToView());
}
