using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Marketplace;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The popup that finds a round's temporary store: roll for it, or pick the number rolled at the table; a 19-20
/// lets the company choose any store it does not already have. Confirming saves the store to the latest
/// round and calls <c>chosen</c>.
/// </summary>
public partial class TemporaryStoreSearchViewModel(
    ISeasonFileService seasonFiles,
    IDice dice,
    Func<string, Task> chosen) : ObservableObject
{
    public const string NotRolled = "—";

    private IReadOnlySet<string> _ownedStores = new HashSet<string>();
    private string? _seasonFilePath;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _resultText = NotRolled;

    [ObservableProperty]
    private StoreSearchTone _resultTone;

    [ObservableProperty]
    private bool _isChoosingAny;

    [ObservableProperty]
    private StoreRollOption? _selectedRoll;

    [ObservableProperty]
    private IReadOnlyList<string> _choices = [];

    [ObservableProperty]
    private string? _selectedChoice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _pendingStore;

    public IReadOnlyList<StoreRollOption> RollOptions => StoreRollOption.All;

    /// <summary>
    /// Opens the search when the latest round has no temporary store yet. The company already has
    /// <paramref name="ownedStores"/>; any other store in <paramref name="allStores"/> can be chosen.
    /// </summary>
    public void OpenIfNeeded(string? seasonFilePath, SeasonFile? seasonFile, IReadOnlyCollection<string> ownedStores, IReadOnlyList<string> allStores)
    {
        var latest = SeasonRounds.Latest(seasonFile);
        if (latest is null || !string.IsNullOrWhiteSpace(latest.TemporaryStore))
        {
            IsOpen = false;
            return;
        }

        _seasonFilePath = seasonFilePath;
        _ownedStores = new HashSet<string>(ownedStores, StringComparer.OrdinalIgnoreCase);
        Choices = allStores.Where(store => !_ownedStores.Contains(store)).ToList();
        ResultText = NotRolled;
        ResultTone = StoreSearchTone.Neutral;
        IsChoosingAny = false;
        SelectedRoll = null;
        SelectedChoice = null;
        PendingStore = null;
        IsOpen = true;
    }

    /// <summary>Rolls until a store the company lacks, or a 19-20, comes up.</summary>
    [RelayCommand]
    private void Roll()
    {
        var result = TemporaryStoreRoll.Search(dice.RollD20, _ownedStores);
        Show(result, result.Kind switch
        {
            TemporaryStoreRollKind.Found => $"Rolled {result.Roll} — {result.Store}",
            TemporaryStoreRollKind.ChooseAny => $"Rolled {result.Roll} — choose any store",
            _ => "No unowned store reached after rerolls — choose any"
        });
    }

    partial void OnSelectedRollChanged(StoreRollOption? value)
    {
        if (value is null)
        {
            return;
        }

        var result = TemporaryStoreRoll.For(value.Roll, _ownedStores);
        Show(result, result.Kind switch
        {
            TemporaryStoreRollKind.Found => $"{value.Roll} — {result.Store}",
            TemporaryStoreRollKind.AlreadyOwned => $"{value.Roll} — {result.Store} (already owned — pick another)",
            TemporaryStoreRollKind.ChooseAny => "Picked 19-20 — choose any store",
            _ => "Invalid selection"
        });
    }

    partial void OnSelectedChoiceChanged(string? value)
    {
        if (IsChoosingAny && !string.IsNullOrWhiteSpace(value))
        {
            PendingStore = value;
        }
    }

    private void Show(TemporaryStoreRoll result, string text)
    {
        ResultText = text;
        ResultTone = result.Kind switch
        {
            TemporaryStoreRollKind.Found => StoreSearchTone.Found,
            TemporaryStoreRollKind.ChooseAny or TemporaryStoreRollKind.Exhausted => StoreSearchTone.Choose,
            _ => StoreSearchTone.Problem
        };
        IsChoosingAny = result.Kind is TemporaryStoreRollKind.ChooseAny or TemporaryStoreRollKind.Exhausted;
        SelectedChoice = null;
        PendingStore = result.Kind == TemporaryStoreRollKind.Found ? result.Store : null;
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(PendingStore);

    /// <summary>Makes the pending store this round's temporary store.</summary>
    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        if (PendingStore is not { } store)
        {
            return;
        }

        await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round => round.TemporaryStore = store);
        IsOpen = false;
        await chosen(store);
    }
}
