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
            var document = CurrentLocked(new DocumentKey(kind, documentId));
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
                _state.Documents.Add(new SyncedDocument
                {
                    Kind = kind,
                    DocumentId = documentId,
                    RelativePath = relativePath,
                    Pending = SyncOperation.Save,
                    Change = 1
                });
            }

            Save();
        }
    }

    /// <summary>
    /// Starts again from nothing when another account signs in: every file is sent to it afresh, and the old
    /// account's conflicts are dropped. The first account to sign in keeps what was noted before.
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
                    .Select(document => document with { Revision = 0, Pending = SyncOperation.Save, Change = document.Change + 1 })
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
    private SyncedDocument CurrentLocked(DocumentKey key) =>
        FindLocked(key) ?? new SyncedDocument { Kind = key.Kind, DocumentId = key.DocumentId };

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
