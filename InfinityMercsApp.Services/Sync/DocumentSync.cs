using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.SignIn;

namespace InfinityMercsApp.Services.Sync;

/// <summary>
/// Keeps the signed-in user's companies and seasons the same here and online. A sync sends the outbox, then fetches
/// what other devices changed. A file changed both here and elsewhere since the last sync is held as a
/// <see cref="SyncConflict"/> until the user picks a version (<see cref="ResolveAsync"/>); nothing is overwritten
/// without asking.
/// </summary>
public sealed class DocumentSync
{
    private readonly SyncLedger _ledger;
    private readonly IUserDocumentsApi _api;
    private readonly ISignInService _signIn;
    private readonly LocalRecordFiles _files;
    private readonly SemaphoreSlim _running = new(1, 1);

    public DocumentSync(SyncLedger ledger, IUserDocumentsApi api, ISignInService signIn, RecordPaths paths)
    {
        _ledger = ledger;
        _api = api;
        _signIn = signIn;
        _files = new LocalRecordFiles(paths);
    }

    /// <summary>Sends what changed here and fetches what changed elsewhere; never throws for a failed request.</summary>
    public async Task<SyncReport> RunAsync(CancellationToken cancellationToken = default)
    {
        await _running.WaitAsync(cancellationToken);
        try
        {
            if (await _signIn.GetAccountAsync(cancellationToken) is not { } account ||
                await _signIn.GetAccessTokenAsync(cancellationToken) is not { } token)
            {
                return SyncReport.SignedOut;
            }

            _ledger.UseAccount(account.Username);
            foreach (var file in _files.All())
            {
                _ledger.Track(file.Kind, file.DocumentId, file.FilePath);
            }

            var sent = 0;
            foreach (var document in _ledger.Documents.Where(document => document.Pending != SyncOperation.None && !_ledger.IsInConflict(document.Key)))
            {
                var step = document.Kind == UserDocumentKind.Company
                    ? await SendAsync<SavedCompanyFile>(document, token, cancellationToken)
                    : await SendAsync<SeasonFile>(document, token, cancellationToken);
                if (Stopped(step) is { } stopped)
                {
                    return new SyncReport(stopped, sent, 0, _ledger.Conflicts);
                }

                sent += step == SyncStep.Moved ? 1 : 0;
            }

            var companies = await ReceiveAsync<SavedCompanyFile>(UserDocumentKind.Company, token, cancellationToken);
            if (Stopped(companies.Step) is { } stoppedFetching)
            {
                return new SyncReport(stoppedFetching, sent, companies.Files, _ledger.Conflicts);
            }

            var seasons = await ReceiveAsync<SeasonFile>(UserDocumentKind.Season, token, cancellationToken);
            return new SyncReport(Stopped(seasons.Step) ?? SyncRunStatus.Synced, sent, companies.Files + seasons.Files, _ledger.Conflicts);
        }
        finally
        {
            _running.Release();
        }
    }

    /// <summary>
    /// Ends a conflict with the user's choice; the next sync sends this device's version when that is the one kept.
    /// False when the other device's version couldn't be fetched, and the conflict stays.
    /// </summary>
    public async Task<bool> ResolveAsync(SyncConflict conflict, SyncChoice choice, CancellationToken cancellationToken = default)
    {
        await _running.WaitAsync(cancellationToken);
        try
        {
            var document = _ledger.Find(conflict.Key);
            if (choice == SyncChoice.KeepThisDevice)
            {
                var existsHere = document is not null && File.Exists(_ledger.FullPathOf(document));
                if (!existsHere && conflict.OtherDeleted)
                {
                    _ledger.ResolveByForgetting(conflict.Key);
                }
                else
                {
                    _ledger.Resolve(conflict.Key, conflict.OtherRevision, existsHere ? SyncOperation.Save : SyncOperation.Delete);
                }

                return true;
            }

            if (conflict.OtherDeleted)
            {
                if (document is not null)
                {
                    LocalRecordFiles.Delete(_ledger.FullPathOf(document));
                }

                _ledger.ResolveByForgetting(conflict.Key);
                return true;
            }

            return await _signIn.GetAccessTokenAsync(cancellationToken) is { } token &&
                   (conflict.Kind == UserDocumentKind.Company
                       ? await TakeOtherAsync<SavedCompanyFile>(conflict, document, token, cancellationToken)
                       : await TakeOtherAsync<SeasonFile>(conflict, document, token, cancellationToken));
        }
        finally
        {
            _running.Release();
        }
    }

    private async Task<SyncStep> SendAsync<TContent>(SyncedDocument document, string token, CancellationToken cancellationToken)
        where TContent : class
    {
        var filePath = _ledger.FullPathOf(document);
        if (document.Pending == SyncOperation.Save)
        {
            if (LocalRecordFiles.Read<TContent>(filePath) is { } content)
            {
                // The API would refuse it; it is sent once a save puts it right.
                if (LocalRecordFiles.ProblemsWith(content) is { Count: > 0 } problems)
                {
                    Console.Error.WriteLine($"'{filePath}' isn't sent: {string.Join(" ", problems)}");
                    _ledger.Refused(document.Key, document.Change);
                    return SyncStep.Unchanged;
                }

                return await SaveAsync(document, content, token, cancellationToken);
            }

            if (File.Exists(filePath))
            {
                // Unreadable: sent again once it is saved again.
                _ledger.Refused(document.Key, document.Change);
                return SyncStep.Unchanged;
            }

            // Deleted without the outbox hearing of it: a file never sent is simply forgotten.
            if (document.Revision == 0)
            {
                _ledger.Forget(document.Key);
                return SyncStep.Unchanged;
            }
        }

        return await DeleteAsync<TContent>(document, token, cancellationToken);
    }

    private async Task<SyncStep> SaveAsync<TContent>(SyncedDocument document, TContent content, string token, CancellationToken cancellationToken)
        where TContent : class
    {
        var write = new UserDocumentWrite<TContent> { BaseRevision = document.Revision, Content = content };
        var reply = await _api.SaveAsync(document.Kind, document.DocumentId, write, token, cancellationToken);
        switch (reply.Status, reply.Value)
        {
            case (UserDocumentsStatus.Ok, { } saved):
                _ledger.Sent(document.Key, document.Change, saved.Revision);
                return SyncStep.Moved;
            case (UserDocumentsStatus.Conflict, { Deleted: false, Content: { } other } current) when LocalRecordFiles.Same(content, other):
                // Another device, or an earlier send whose answer was lost, already put this version online.
                _ledger.Sent(document.Key, document.Change, current.Revision);
                return SyncStep.Unchanged;
            case (UserDocumentsStatus.Conflict, { } current):
                _ledger.AddConflict(ConflictOf(document, current, LocalRecordFiles.NameOf(content), deletedHere: false));
                return SyncStep.Unchanged;
            default:
                return Failed(document, reply.Status);
        }
    }

    private async Task<SyncStep> DeleteAsync<TContent>(SyncedDocument document, string token, CancellationToken cancellationToken)
        where TContent : class
    {
        var reply = await _api.DeleteAsync<TContent>(document.Kind, document.DocumentId, document.Revision, token, cancellationToken);
        switch (reply.Status, reply.Value)
        {
            case (UserDocumentsStatus.Ok or UserDocumentsStatus.NotFound, _):
            case (UserDocumentsStatus.Conflict, { Deleted: true }):
                _ledger.Forget(document.Key);
                return SyncStep.Moved;
            case (UserDocumentsStatus.Conflict, { } current):
                _ledger.AddConflict(ConflictOf(document, current, LocalRecordFiles.NameOf(current.Content), deletedHere: true));
                return SyncStep.Unchanged;
            default:
                return Failed(document, reply.Status);
        }
    }

    // Fetches the kind's files that changed elsewhere, and deletes those deleted elsewhere. Files waiting to be sent,
    // or held for the user, are left alone.
    private async Task<ReceiveResult> ReceiveAsync<TContent>(UserDocumentKind kind, string token, CancellationToken cancellationToken)
        where TContent : class
    {
        var listing = await _api.ListAsync<TContent>(kind, token, cancellationToken);
        if (listing is not { Status: UserDocumentsStatus.Ok, Value: { } views })
        {
            return new ReceiveResult(StepFor(listing.Status), 0);
        }

        var files = 0;
        foreach (var view in views)
        {
            var key = new DocumentKey(kind, view.DocumentId);
            if (!NeedsFetching(key, view.Revision))
            {
                continue;
            }

            if (view.Deleted)
            {
                if (_ledger.Find(key) is { } document)
                {
                    LocalRecordFiles.Delete(_ledger.FullPathOf(document));
                    _ledger.Forget(key);
                    files++;
                }

                continue;
            }

            var reply = await _api.GetAsync<TContent>(kind, view.DocumentId, token, cancellationToken);
            if (reply is not { Status: UserDocumentsStatus.Ok, Value: { Content: { } content } current })
            {
                if (StepFor(reply.Status) is SyncStep.Offline or SyncStep.SignedOut)
                {
                    return new ReceiveResult(StepFor(reply.Status), files);
                }

                continue;
            }

            // Saved here while it was being fetched: this device's save goes out next time instead.
            if (!NeedsFetching(key, current.Revision))
            {
                continue;
            }

            var filePath = _ledger.Find(key) is { } known ? _ledger.FullPathOf(known) : _files.NewPathFor(kind, view.DocumentId, content);
            LocalRecordFiles.Write(filePath, content);
            _ledger.Received(key, filePath, current.Revision);
            files++;
        }

        return new ReceiveResult(SyncStep.Unchanged, files);
    }

    private async Task<bool> TakeOtherAsync<TContent>(SyncConflict conflict, SyncedDocument? document, string token, CancellationToken cancellationToken)
        where TContent : class
    {
        var reply = await _api.GetAsync<TContent>(conflict.Kind, conflict.DocumentId, token, cancellationToken);
        if (reply is not { Status: UserDocumentsStatus.Ok, Value: { } current })
        {
            return false;
        }

        if (current is not { Deleted: false, Content: { } content })
        {
            if (document is not null)
            {
                LocalRecordFiles.Delete(_ledger.FullPathOf(document));
            }

            _ledger.ResolveByForgetting(conflict.Key);
            return true;
        }

        var filePath = document is not null && document.Pending != SyncOperation.Delete
            ? _ledger.FullPathOf(document)
            : _files.NewPathFor(conflict.Kind, conflict.DocumentId, content);
        LocalRecordFiles.Write(filePath, content);
        _ledger.Received(conflict.Key, filePath, current.Revision);
        _ledger.Resolve(conflict.Key, current.Revision, SyncOperation.None);
        return true;
    }

    // Whether the version online is newer than this device's, which has nothing waiting to be sent.
    private bool NeedsFetching(DocumentKey key, int revision)
    {
        if (_ledger.IsInConflict(key))
        {
            return false;
        }

        return _ledger.Find(key) is not { } document || (document.Pending == SyncOperation.None && document.Revision < revision);
    }

    private SyncStep Failed(SyncedDocument document, UserDocumentsStatus status)
    {
        var step = StepFor(status);
        if (step == SyncStep.Unchanged)
        {
            Console.Error.WriteLine($"The API refused {document.Kind} {document.DocumentId} ({status}); it is sent again once it is saved again.");
            _ledger.Refused(document.Key, document.Change);
        }

        return step;
    }

    private static SyncStep StepFor(UserDocumentsStatus status) => status switch
    {
        UserDocumentsStatus.Unauthorized => SyncStep.SignedOut,
        UserDocumentsStatus.Unavailable => SyncStep.Offline,
        _ => SyncStep.Unchanged
    };

    private static SyncRunStatus? Stopped(SyncStep step) => step switch
    {
        SyncStep.Offline => SyncRunStatus.Offline,
        SyncStep.SignedOut => SyncRunStatus.SignedOut,
        _ => null
    };

    private static SyncConflict ConflictOf<TContent>(SyncedDocument document, UserDocumentView<TContent> current, string name, bool deletedHere)
        where TContent : class => new()
    {
        Kind = document.Kind,
        DocumentId = document.DocumentId,
        Name = name,
        OtherRevision = current.Revision,
        OtherUpdatedAt = current.UpdatedAt,
        OtherDeleted = current.Deleted,
        DeletedHere = deletedHere
    };
}
