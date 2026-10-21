using InfinityMercsApp.Presentation.Sync;
using InfinityMercsApp.Services.Dialogs;

namespace InfinityMercsApp.Services.Sync;

/// <inheritdoc/>
/// <remarks>
/// Syncs once the app has opened, a few seconds after a save (so a burst of saves goes out together), and every
/// fifteen minutes. A conflict the user puts off isn't asked about again until they sync from the settings page.
/// </remarks>
internal sealed class SyncCoordinator(DocumentSync sync, SyncLedger ledger, IDialogService dialogs) : ISyncCoordinator
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(3);

    private readonly HashSet<DocumentKey> _putOff = [];
    private readonly SemaphoreSlim _asking = new(1, 1);
    private int _started;

    /// <inheritdoc/>
    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 0)
        {
            _ = RunAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<SyncReport> SyncNowAsync()
    {
        lock (_putOff)
        {
            _putOff.Clear();
        }

        return await SyncAndAskAsync();
    }

    private async Task RunAsync()
    {
        while (true)
        {
            await SyncAndAskAsync();
            using (var interval = new CancellationTokenSource(Interval))
            {
                try
                {
                    await ledger.WaitForChangeAsync(interval.Token);
                }
                catch (OperationCanceledException)
                {
                    // Time for the regular sync.
                }
            }

            await Task.Delay(SettleTime);
        }
    }

    private async Task<SyncReport> SyncAndAskAsync()
    {
        try
        {
            var report = await sync.RunAsync();
            return await AskAsync(report.Conflicts) ? await sync.RunAsync() : report;
        }
        catch (Exception ex)
        {
            // Sync runs in the background; a failure waits for the next one instead of ending them all.
            Console.Error.WriteLine($"Sync failed: {ex.Message}");
            return new SyncReport(SyncRunStatus.Offline, 0, 0, ledger.Conflicts);
        }
    }

    // Asks about each conflict not put off; true when one was resolved, so the choice is sent straight away.
    private async Task<bool> AskAsync(IReadOnlyList<SyncConflict> conflicts)
    {
        if (!await _asking.WaitAsync(0))
        {
            return false;
        }

        try
        {
            var resolved = false;
            foreach (var conflict in conflicts)
            {
                lock (_putOff)
                {
                    if (_putOff.Contains(conflict.Key))
                    {
                        continue;
                    }
                }

                var question = SyncConflictQuestion.For(conflict);
                var answer = await MainThread.InvokeOnMainThreadAsync(() =>
                    dialogs.ChooseAsync(question.Question, SyncConflictQuestion.DecideLater, question.Answers));
                if (question.ChoiceFor(answer) is { } choice && await sync.ResolveAsync(conflict, choice))
                {
                    resolved = true;
                    continue;
                }

                lock (_putOff)
                {
                    _putOff.Add(conflict.Key);
                }
            }

            return resolved;
        }
        finally
        {
            _asking.Release();
        }
    }
}
