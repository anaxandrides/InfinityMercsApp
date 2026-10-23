using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Services.Records;

namespace InfinityMercsApp.Services.Sync;

/// <summary>
/// The outbox and what this device knows of the user's files online, kept in the MercenaryRecords folder so it moves
/// with the files it describes. Saves and deletions are noted whether or not anyone is signed in, and sent at the next
/// sync. Safe to use from any thread.
/// </summary>
public sealed class SyncLedger : ISyncOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    private readonly RecordPaths _paths;
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _changed = new(0);
    private SyncState _state;

    public SyncLedger(RecordPaths paths)
    {
        _paths = paths;
        _state = Load(paths.SyncStatePath);
    }

    public string Account
    {
        get
        {
            lock (_lock)
            {
                return _state.Account;
            }
        }
    }

    public IReadOnlyList<SyncedDocument> Documents
    {
        get
        {
            lock (_lock)
            {
                return _state.Documents.ToList();
            }
        }
    }

    public IReadOnlyList<SyncConflict> Conflicts
    {
        get
        {
            lock (_lock)
            {
                return _state.Conflicts.ToList();
            }
        }
    }

    /// <inheritdoc/>
    public void RecordSave(UserDocumentKind kind, string documentId, string filePath)
    {
        var relativePath = RelativePathOf(filePath);
        lock (_lock)
        {
            var key = new DocumentKey(kind, documentId);
            var document = FindLocked(key) ?? NewLocked(key);
            Put(document, document with { RelativePath = relativePath, Pending = SyncOperation.Save, Change = document.Change + 1 });
        }

        _changed.Release();
    }

    /// <inheritdoc/>
    public void RecordDelete(UserDocumentKind kind, string documentId)
    {
        lock (_lock)
        {
            if (FindLocked(new DocumentKey(kind, documentId)) is not { } document)
            {
                return;
            }

            // A file never sent has nothing online to delete.
            if (document.Revision == 0)
            {
                _state.Documents.Remove(document);
            }
            else
            {
                Replace(document, document with { Pending = SyncOperation.Delete, Change = document.Change + 1 });
            }

            Save();
        }

        _changed.Release();
    }

    /// <summary>Waits until a save or deletion is noted.</summary>
    public async Task WaitForChangeAsync(CancellationToken cancellationToken)
    {
        await _changed.WaitAsync(cancellationToken);
        while (_changed.Wait(0))
        {
        }
    }

    public SyncedDocument? Find(DocumentKey key)
    {
        lock (_lock)
        {
            return FindLocked(key);
        }
    }

    public bool IsInConflict(DocumentKey key)
    {
        lock (_lock)
        {
            return _state.Conflicts.Any(conflict => conflict.Key == key);
        }
    }

    /// <summary>The file's full path.</summary>
    public string FullPathOf(SyncedDocument document) => Path.Combine(_paths.CompaniesDirectory, document.RelativePath);

    /// <summary>
    /// Notes a file found on disk: one the ledger doesn't know yet, as one written before signing in, is queued to be
    /// sent; one it knows has its path brought up to date.
    /// </summary>
    public void Track(UserDocumentKind kind, string documentId, string filePath)
    {
        var relativePath = RelativePathOf(filePath);
        lock (_lock)
        {
            var key = new DocumentKey(kind, documentId);
            if (FindLocked(key) is { } document)
            {
                if (document.RelativePath == relativePath)
                {
                    return;
                }

                Replace(document, document with { RelativePath = relativePath });
            }
            else
            {
                // Found on the device without the outbox hearing of it: whose it is is the user's to say.
                _state.Documents.Add(new SyncedDocument
                {
                    Kind = kind,
                    DocumentId = documentId,
                    RelativePath = relativePath,
                    Pending = SyncOperation.Save,
                    Change = 1,
                    Ownership = SyncOwnership.Unclaimed
                });
            }

            Save();
        }
    }

    /// <summary>
    /// Starts again from nothing when another account signs in: every file becomes unclaimed, to be sent to the new
    /// account only if the user adds it, and the old account's conflicts are dropped. The first account to sign in
    /// keeps what was noted before, files saved while signed out still unclaimed.
    /// </summary>
    public void UseAccount(string account)
    {
        lock (_lock)
        {
            if (_state.Account == account)
            {
                return;
            }

            var documents = string.IsNullOrEmpty(_state.Account)
                ? _state.Documents
                : _state.Documents
                    .Where(document => document.Pending != SyncOperation.Delete)
                    .Select(document => document with
                    {
                        Revision = 0,
                        Pending = SyncOperation.Save,
                        Change = document.Change + 1,
                        Ownership = SyncOwnership.Unclaimed
                    })
                    .ToList();
            _state = new SyncState
            {
                Account = account,
                Documents = documents,
                Conflicts = string.IsNullOrEmpty(_state.Account) ? _state.Conflicts : []
            };
            Save();
        }
    }

    /// <summary>How many companies and seasons wait for the user to add them to the account.</summary>
    public FileCounts Unclaimed()
    {
        lock (_lock)
        {
            var unclaimed = _state.Documents.Where(document => document.Ownership == SyncOwnership.Unclaimed).ToList();
            return new FileCounts(
                unclaimed.Count(document => document.Kind == UserDocumentKind.Company),
                unclaimed.Count(document => document.Kind == UserDocumentKind.Season));
        }
    }

    /// <summary>How many companies and seasons this device knows are in the account online and not being deleted.</summary>
    public FileCounts Online()
    {
        lock (_lock)
        {
            var online = _state.Documents
                .Where(document => document is { Ownership: SyncOwnership.Claimed, Revision: > 0, Pending: not SyncOperation.Delete })
                .ToList();
            return new FileCounts(
                online.Count(document => document.Kind == UserDocumentKind.Company),
                online.Count(document => document.Kind == UserDocumentKind.Season));
        }
    }

    /// <summary>Adds every unclaimed file to the signed-in account, to be sent at the next sync.</summary>
    public void ClaimAll()
    {
        SetUnclaimed(SyncOwnership.Claimed);
        _changed.Release();
    }

    /// <summary>Keeps every unclaimed file on this device only; it is never sent.</summary>
    public void KeepUnclaimedOnDevice() => SetUnclaimed(SyncOwnership.DeviceOnly);

    /// <summary>The file reached <paramref name="revision"/> online; it is done unless it changed again meanwhile.</summary>
    public void Sent(DocumentKey key, int change, int revision)
    {
        lock (_lock)
        {
            var document = CurrentLocked(key);
            Put(document, document with
            {
                Revision = revision,
                Pending = document.Change == change ? SyncOperation.None : document.Pending
            });
        }
    }

    /// <summary>The API won't take the file as it is; it is sent again only once it is saved again.</summary>
    public void Refused(DocumentKey key, int change)
    {
        lock (_lock)
        {
            if (FindLocked(key) is { } document && document.Change == change)
            {
                Put(document, document with { Pending = SyncOperation.None });
            }
        }
    }

    /// <summary>This device's copy is now <paramref name="revision"/> online, fetched or already the same.</summary>
    public void Received(DocumentKey key, string filePath, int revision)
    {
        var relativePath = RelativePathOf(filePath);
        lock (_lock)
        {
            var document = CurrentLocked(key);
            Put(document, document with { RelativePath = relativePath, Revision = revision, Pending = SyncOperation.None });
        }
    }

    /// <summary>Forgets the file, once it is deleted here and online.</summary>
    public void Forget(DocumentKey key)
    {
        lock (_lock)
        {
            if (FindLocked(key) is { } document)
            {
                _state.Documents.Remove(document);
                Save();
            }
        }
    }

    /// <summary>Holds the file back until the user picks a version.</summary>
    public void AddConflict(SyncConflict conflict)
    {
        lock (_lock)
        {
            _state.Conflicts.RemoveAll(existing => existing.Key == conflict.Key);
            _state.Conflicts.Add(conflict);
            Save();
        }
    }

    /// <summary>
    /// Ends a conflict: the file builds on <paramref name="revision"/> online from now on, and sends
    /// <paramref name="pending"/> next.
    /// </summary>
    public void Resolve(DocumentKey key, int revision, SyncOperation pending)
    {
        lock (_lock)
        {
            _state.Conflicts.RemoveAll(conflict => conflict.Key == key);
            if (FindLocked(key) is { } document)
            {
                Replace(document, document with { Revision = revision, Pending = pending, Change = document.Change + 1 });
            }

            Save();
        }

        if (pending != SyncOperation.None)
        {
            _changed.Release();
        }
    }

    /// <summary>Ends a conflict by dropping the file, which is deleted both here and online.</summary>
    public void ResolveByForgetting(DocumentKey key)
    {
        lock (_lock)
        {
            _state.Conflicts.RemoveAll(conflict => conflict.Key == key);
            _state.Documents.RemoveAll(document => document.Key == key);
            Save();
        }
    }

    // The file's entry, or a new one not yet in the ledger.
    private SyncedDocument CurrentLocked(DocumentKey key) => FindLocked(key) ?? NewLocked(key);

    // A file new to the ledger: the signed-in account's, unless nobody has signed in on this device yet.
    private SyncedDocument NewLocked(DocumentKey key) => new()
    {
        Kind = key.Kind,
        DocumentId = key.DocumentId,
        Ownership = string.IsNullOrEmpty(_state.Account) ? SyncOwnership.Unclaimed : SyncOwnership.Claimed
    };

    private void SetUnclaimed(SyncOwnership ownership)
    {
        lock (_lock)
        {
            for (var i = 0; i < _state.Documents.Count; i++)
            {
                if (_state.Documents[i].Ownership == SyncOwnership.Unclaimed)
                {
                    _state.Documents[i] = _state.Documents[i] with { Ownership = ownership };
                }
            }

            Save();
        }
    }

    // Puts the changed entry in place of the current one, or adds it, and saves the ledger.
    private void Put(SyncedDocument current, SyncedDocument changed)
    {
        var index = _state.Documents.IndexOf(current);
        if (index >= 0)
        {
            _state.Documents[index] = changed;
        }
        else
        {
            _state.Documents.Add(changed);
        }

        Save();
    }

    private SyncedDocument? FindLocked(DocumentKey key) => _state.Documents.FirstOrDefault(document => document.Key == key);

    private void Replace(SyncedDocument document, SyncedDocument changed) =>
        _state.Documents[_state.Documents.IndexOf(document)] = changed;

    private string RelativePathOf(string filePath) => Path.GetRelativePath(_paths.CompaniesDirectory, filePath);

    // Written to a new file and moved over the old, so a crash mid-write can't leave half a ledger.
    private void Save()
    {
        try
        {
            var path = _paths.SyncStatePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _paths.CompaniesDirectory);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(_state, JsonOptions));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"The sync ledger couldn't be saved: {ex.Message}");
        }
    }

    private static SyncState Load(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<SyncState>(File.ReadAllText(path), JsonOptions) ?? new SyncState() : new SyncState();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Without the ledger every file is sent again, and any already online compares as the same.
            Console.Error.WriteLine($"The sync ledger couldn't be read: {ex.Message}");
            return new SyncState();
        }
    }
}
