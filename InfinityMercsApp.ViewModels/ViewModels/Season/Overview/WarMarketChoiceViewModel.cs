using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Marketplace;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Presentation.Season.Overview;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The popup that picks this round's War Market: one of the company's faction markets, or, after the first
/// contract, a roll (or the number rolled at the table). A rolled faction market is rolled again, and 19-20
/// lets the company choose any other market. Confirming saves the market where the season is shopping now
/// and calls <c>chosen</c>; until then the popup can be put off and opened again.
/// </summary>
public partial class WarMarketChoiceViewModel(
    ISeasonFileService seasonFiles,
    IDice dice,
    Func<string, Task> chosen) : ObservableObject
{
    public const string NotRolled = "—";

    private IReadOnlySet<string> _factionMarkets = new HashSet<string>();
    private string? _seasonFilePath;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    /// <summary>No War Market is chosen for this round yet.</summary>
    [ObservableProperty]
    public partial bool IsNeeded { get; set; }

    /// <summary>A War Market can be rolled for: a contract has been played.</summary>
    [ObservableProperty]
    public partial bool CanRoll { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFactionMarkets))]
    public partial IReadOnlyList<string> FactionMarketChoices { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedFactionMarket { get; set; }

    [ObservableProperty]
    public partial string ResultText { get; set; } = NotRolled;

    [ObservableProperty]
    public partial StoreSearchTone ResultTone { get; set; }

    [ObservableProperty]
    public partial bool IsChoosingAny { get; set; }

    [ObservableProperty]
    public partial StoreRollOption? SelectedRoll { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> Choices { get; set; } = [];

    [ObservableProperty]
    public partial string? SelectedChoice { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial string? PendingStore { get; set; }

    public IReadOnlyList<StoreRollOption> RollOptions => StoreRollOption.All;

    public bool HasFactionMarkets => FactionMarketChoices.Count > 0;

    /// <summary>
    /// Gets ready for the season as it stands: needed when this round has no War Market yet. The company's
    /// own markets are <paramref name="factionMarkets"/>; a 19-20 offers the rest of <paramref name="allWarMarkets"/>.
    /// The popup stays shut until <see cref="OpenCommand"/>.
    /// </summary>
    public void Prepare(string? seasonFilePath, SeasonFile? seasonFile, IReadOnlyList<string> factionMarkets, IReadOnlyList<string> allWarMarkets)
    {
        _seasonFilePath = seasonFilePath;
        IsNeeded = seasonFile is not null && WarMarkets.Current(seasonFile) is null;
        CanRoll = WarMarkets.CanRoll(seasonFile);
        _factionMarkets = new HashSet<string>(factionMarkets, StringComparer.OrdinalIgnoreCase);
        FactionMarketChoices = factionMarkets;
        Choices = allWarMarkets.Where(store => !_factionMarkets.Contains(store)).ToList();
        if (!IsNeeded)
        {
            IsOpen = false;
        }
    }

    /// <summary>Opens the popup afresh, when a War Market is still needed.</summary>
    [RelayCommand]
    private void Open()
    {
        if (!IsNeeded)
        {
            return;
        }

        SelectedFactionMarket = null;
        ResultText = NotRolled;
        ResultTone = StoreSearchTone.Neutral;
        IsChoosingAny = false;
        SelectedRoll = null;
        SelectedChoice = null;
        PendingStore = null;
        IsOpen = true;
    }

    /// <summary>Puts the choice off; the market tab can open it again.</summary>
    [RelayCommand]
    private void Close() => IsOpen = false;

    partial void OnSelectedFactionMarketChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        ResultText = $"Taking {value}";
        ResultTone = StoreSearchTone.Found;
        IsChoosingAny = false;
        SelectedRoll = null;
        PendingStore = value;
    }

    /// <summary>Rolls until another faction's market, or a 19-20, comes up.</summary>
    [RelayCommand]
    private void Roll()
    {
        if (!CanRoll)
        {
            return;
        }

        var result = WarMarketRoll.Search(dice, _factionMarkets);
        Show(result, result.Kind switch
        {
            WarMarketRollKind.Found => $"Rolled {result.Roll} — {result.Store}",
            WarMarketRollKind.ChooseAny => $"Rolled {result.Roll} — choose any market",
            _ => "Only your own markets came up after rerolls — choose any"
        });
    }

    partial void OnSelectedRollChanged(StoreRollOption? value)
    {
        if (value is null || !CanRoll)
        {
            return;
        }

        var result = WarMarketRoll.For(value.Roll, _factionMarkets);
        Show(result, result.Kind switch
        {
            WarMarketRollKind.Found => $"{value.Roll} — {result.Store}",
            WarMarketRollKind.FactionMarket => $"{value.Roll} — {result.Store} (your own market — roll again)",
            WarMarketRollKind.ChooseAny => "Picked 19-20 — choose any market",
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

    private void Show(WarMarketRoll result, string text)
    {
        SelectedFactionMarket = null;
        ResultText = text;
        ResultTone = result.Kind switch
        {
            WarMarketRollKind.Found => StoreSearchTone.Found,
            WarMarketRollKind.ChooseAny or WarMarketRollKind.Exhausted => StoreSearchTone.Choose,
            _ => StoreSearchTone.Problem
        };
        IsChoosingAny = result.Kind is WarMarketRollKind.ChooseAny or WarMarketRollKind.Exhausted;
        SelectedChoice = null;
        PendingStore = result.Kind == WarMarketRollKind.Found ? result.Store : null;
    }

    private bool CanConfirm() => !string.IsNullOrWhiteSpace(PendingStore);

    /// <summary>Makes the pending market this round's War Market.</summary>
    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        if (PendingStore is not { } store)
        {
            return;
        }

        await seasonFiles.UpdateAsync(_seasonFilePath, season => SeasonRounds.CurrentMarketplace(season).WarMarket = store);
        IsOpen = false;
        IsNeeded = false;
        await chosen(store);
    }
}
