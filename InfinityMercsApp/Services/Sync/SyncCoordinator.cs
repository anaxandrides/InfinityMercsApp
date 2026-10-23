using InfinityMercsApp.Presentation.Sync;
using InfinityMercsApp.Services.Dialogs;

namespace InfinityMercsApp.Services.Sync;

/// <inheritdoc/>
/// <remarks>
/// Syncs once the app has opened, a few seconds after a save (so a burst of saves goes out together), and every
/// fifteen minutes. A conflict the user puts off isn't asked about again until they sync from the settings page, and
/// nor are files on the device that aren't in the account: nothing is sent to an account until the user adds it.
/// </remarks>
internal sealed class SyncCoordinator(DocumentSync sync, SyncLedger ledger, IDialogService dialogs) : ISyncCoordinator
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(3);

    private readonly HashSet<DocumentKey> _putOff = [];
    private readonly SemaphoreSlim _asking = new(1, 1);
    private bool _unclaimedPutOff;
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
            _unclaimedPutOff = false;
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
            var claimed = await AskAboutUnclaimedAsync(report);
            var resolved = await AskAsync(report.Conflicts);
            return claimed || resolved ? await sync.RunAsync() : report;
        }
        catch (Exception ex)
        {
            // Sync runs in the background; a failure waits for the next one instead of ending them all.
            Console.Error.WriteLine($"Sync failed: {ex.Message}");
            return new SyncReport(SyncRunStatus.Offline, 0, 0, ledger.Conflicts, ledger.Unclaimed());
        }
    }

    // Asks whether to add the device's unclaimed files to the account; true when they were added, so they're sent now.
    private async Task<bool> AskAboutUnclaimedAsync(SyncReport report)
    {
        lock (_putOff)
        {
            if (report.Status != SyncRunStatus.Synced || report.Unclaimed.Total == 0 || _unclaimedPutOff)
            {
                return false;
            }
        }

        if (!await _asking.WaitAsync(0))
        {
            return false;
        }

        try
        {
            var question = UnclaimedFilesQuestion.For(report.Unclaimed);
            var answer = await MainThread.InvokeOnMainThreadAsync(() =>
                dialogs.ChooseAsync(question.Question, UnclaimedFilesQuestion.NotNow, question.Answers));
            switch (UnclaimedFilesQuestion.ChoiceFor(answer))
            {
                case ClaimChoice.AddToAccount:
                    ledger.ClaimAll();
                    return true;
                case ClaimChoice.KeepOnDevice:
                    ledger.KeepUnclaimedOnDevice();
                    return false;
                default:
                    lock (_putOff)
                    {
                        _unclaimedPutOff = true;
                    }

                    return false;
            }
        }
        finally
        {
            _asking.Release();
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
