using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Loot;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Season.Loot;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Season.Loot.Items;

namespace InfinityMercsApp.ViewModels.Season.Loot;

/// <summary>
/// The company's loot boxes. The player adds a box the organiser gave, then opens it roll by roll: the weapon
/// first, then buffs, each roll taking 1 to 4 dice. Every roll is saved as it is made. Taking the loot puts it in
/// the inventory, and any dice left are given up. Boxes are opened one at a time, in the order they came.
/// </summary>
public partial class LootBoxViewModel(
    INavigationService navigationService,
    ISeasonFileService seasonFiles,
    IDialogService dialogs,
    IDice dice) : ViewModelBase(navigationService)
{
    private readonly LootBoxOpener _opener = new(dice);
    private string _seasonFilePath = string.Empty;

    /// <summary>The roll buttons, 1 to 4 dice.</summary>
    public ObservableCollection<LootDiceChoice> DiceChoices { get; } = [];

    /// <summary>What each roll of the box being opened gave, this visit.</summary>
    public ObservableCollection<string> Log { get; } = [];

    /// <summary>Loot from the boxes already opened.</summary>
    public ObservableCollection<LootRewardItem> Rewards { get; } = [];

    /// <summary>A box is being opened.</summary>
    [ObservableProperty]
    public partial bool HasBox { get; set; }

    /// <summary>"Black Market Cache (Rare, 6 dice)".</summary>
    [ObservableProperty]
    public partial string BoxTitle { get; set; } = string.Empty;

    /// <summary>"4 dice left · 1 free roll".</summary>
    [ObservableProperty]
    public partial string DiceText { get; set; } = string.Empty;

    /// <summary>What the next roll is for.</summary>
    [ObservableProperty]
    public partial string StepText { get; set; } = string.Empty;

    /// <summary>The loot so far, or what is still to roll.</summary>
    [ObservableProperty]
    public partial string LootText { get; set; } = string.Empty;

    /// <summary>Boxes waiting behind the one being opened.</summary>
    [ObservableProperty]
    public partial int QueuedBoxes { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RerollLastBuffCommand))]
    public partial bool CanRerollLastBuff { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TakeLootCommand))]
    public partial bool CanTakeLoot { get; set; }

    public bool HasRewards => Rewards.Count > 0;

    public override void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;

    public override async Task InitializeAsync() => Show(await seasonFiles.LoadAsync(_seasonFilePath));

    /// <summary>Adds a box of the tier the player picks; it waits behind any box already being opened.</summary>
    [RelayCommand]
    private async Task AddBoxAsync()
    {
        var choice = await dialogs.ChooseAsync("Loot Box Gained", "Cancel", LootBoxTiers.All.Select(tier => tier.Display).ToList());
        if (LootBoxTiers.All.FirstOrDefault(tier => tier.Display == choice) is not { } picked ||
            await seasonFiles.LoadAsync(_seasonFilePath) is not { } season)
        {
            return;
        }

        season.LootBoxes.Add(LootBoxOpener.New(picked.Tier, SeasonRounds.Current(season)));
        await SaveAsync(season);
    }

    /// <summary>Rolls the weapon or a buff with the picked number of dice.</summary>
    [RelayCommand]
    private async Task RollAsync(LootDiceChoice choice)
    {
        if (await seasonFiles.LoadAsync(_seasonFilePath) is not { } season || Opening(season) is not { } box ||
            !LootBoxOpener.CanRoll(box, choice.Dice))
        {
            return;
        }

        Log.Add(_opener.Roll(box, choice.Dice));
        await SaveAsync(season);
    }

    /// <summary>Rerolls the last buff, which the player finds illegal with the others, for nothing.</summary>
    [RelayCommand(CanExecute = nameof(CanRerollLastBuff))]
    private async Task RerollLastBuffAsync()
    {
        if (await seasonFiles.LoadAsync(_seasonFilePath) is not { } season || Opening(season) is not { } box ||
            !LootBoxOpener.CanRerollLastBuff(box))
        {
            return;
        }

        Log.Add(_opener.RerollLastBuff(box));
        await SaveAsync(season);
    }

    /// <summary>Takes the loot into the inventory, after asking when dice or free rolls are left.</summary>
    [RelayCommand(CanExecute = nameof(CanTakeLoot))]
    private async Task TakeLootAsync()
    {
        if (await seasonFiles.LoadAsync(_seasonFilePath) is not { } season || Opening(season) is not { } box ||
            !LootBoxOpener.CanTake(box))
        {
            return;
        }

        if (LootBoxOpener.MaxDice(box) > 0 &&
            !await dialogs.ConfirmAsync("Take the Loot", $"{Remaining(box)} will be given up. Take the loot now?"))
        {
            return;
        }

        LootBoxOpener.Take(box);
        Log.Clear();
        await SaveAsync(season);
    }

    private async Task SaveAsync(SeasonFile season)
    {
        if (!await seasonFiles.SaveAsync(_seasonFilePath, season))
        {
            await dialogs.ShowAlertAsync("Loot Box", SeasonMessages.NotSaved);
        }

        Show(await seasonFiles.LoadAsync(_seasonFilePath));
    }

    private void Show(SeasonFile? season)
    {
        Rewards.Clear();
        foreach (var item in SeasonLoot.Items(season))
        {
            Rewards.Add(LootRewardItem.From(item));
        }

        OnPropertyChanged(nameof(HasRewards));
        var box = season is null ? null : Opening(season);
        HasBox = box is not null;
        QueuedBoxes = season?.LootBoxes.Count(candidate => !candidate.IsOpened) - (HasBox ? 1 : 0) ?? 0;
        DiceChoices.Clear();
        if (box is null)
        {
            Log.Clear();
            CanRerollLastBuff = false;
            CanTakeLoot = false;
            return;
        }

        BoxTitle = LootBoxTiers.Of(box.Tier).Display;
        DiceText = Remaining(box);
        StepText = LootBoxOpener.MaxDice(box) == 0
            ? "No dice left: take the loot."
            : LootBoxOpener.RollsWeapon(box)
                ? "Roll the weapon: pick how many dice. The last is rolled and the others count as 6s."
                : "Roll a buff, or take the loot as it is.";
        LootText = LootItem.SoFar(box)?.Name ?? "No weapon yet";
        foreach (var count in Enumerable.Range(1, LootRolls.MaxDice))
        {
            DiceChoices.Add(new LootDiceChoice(count, LootBoxOpener.CanRoll(box, count)));
        }

        CanRerollLastBuff = LootBoxOpener.CanRerollLastBuff(box);
        CanTakeLoot = LootBoxOpener.CanTake(box);
    }

    // The first box not yet opened.
    private static SeasonLootBox? Opening(SeasonFile season) => season.LootBoxes.FirstOrDefault(box => !box.IsOpened);

    private static string Remaining(SeasonLootBox box)
    {
        var dice = box.DiceLeft == 1 ? "1 die" : $"{box.DiceLeft} dice";
        return box.FreeRolls == 0 ? $"{dice} left" : $"{dice} left · {box.FreeRolls} free roll{(box.FreeRolls == 1 ? string.Empty : "s")}";
    }
}
