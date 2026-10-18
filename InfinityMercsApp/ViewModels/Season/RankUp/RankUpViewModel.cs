using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Presentation.Season.RankUp;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Rolls;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;

namespace InfinityMercsApp.ViewModels.Season.RankUp;

/// <summary>
/// Spends the perk points troopers earned by ranking up, one at a time. A perk is chosen from a list (tier
/// up to the trooper's rank, following on from what it owns) or rolled for: a d20 for the list, then a d20
/// for the track, which may land one tier higher. A TAG's TAG perk comes from the Mecha tree and is never rolled.
/// A perk that fills a gear slot the trooper already uses overrides that gear, which is lost, once the player agrees.
/// </summary>
public partial class RankUpViewModel(
    INavigationService navigationService,
    ISeasonFileService seasonFiles,
    ICompanyFileService companyFiles,
    CompanyRoster roster,
    IDice dice,
    IDialogService dialogs) : ViewModelBase(navigationService)
{
    /// <summary>Track rolls tried before giving up on a list that offers nothing.</summary>
    public const int MaxTrackRolls = 100;

    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private IReadOnlySet<string> _owned = new HashSet<string>();

    /// <summary>The troopers with perk points still to spend.</summary>
    public ObservableCollection<CompanyViewerUnitListItem> Troopers { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTrooper), nameof(AllSpent))]
    public partial CompanyViewerUnitListItem? Trooper { get; set; }

    [ObservableProperty]
    public partial string RankText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PointsText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRoll))]
    public partial PerkPointKind PointKind { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChoosing), nameof(IsRolling))]
    public partial RankUpMode Mode { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<PerkListChoice> Lists { get; set; } = [];

    [ObservableProperty]
    public partial PerkListChoice? SelectedList { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOptions))]
    public partial IReadOnlyList<PerkOption> Options { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRolledLists))]
    public partial IReadOnlyList<PerkListChoice> RolledLists { get; set; } = [];

    [ObservableProperty]
    public partial string RollText { get; set; } = string.Empty;

    public bool HasTrooper => Trooper is not null;

    /// <summary>Every perk point has been spent.</summary>
    public bool AllSpent => Trooper is null;

    public bool IsChoosing => Mode == RankUpMode.Choose;

    public bool IsRolling => Mode == RankUpMode.Roll;

    /// <summary>TAG perks are chosen, never rolled.</summary>
    public bool CanRoll => PointKind == PerkPointKind.Standard;

    public bool HasOptions => Options.Count > 0;

    public bool HasRolledLists => RolledLists.Count > 0;

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    public override Task InitializeAsync() => IsInitialized ? Task.CompletedTask : ReloadAsync(keepEntry: null);

    // Reads the company and season again and shows the next trooper with points, staying on the same one while it has some.
    private async Task ReloadAsync(int? keepEntry)
    {
        Troopers.Clear();
        try
        {
            if (companyFiles.Exists(_companyFilePath) && await companyFiles.LoadAsync(_companyFilePath) is { } company)
            {
                var season = await seasonFiles.LoadAsync(_seasonFilePath);
                foreach (var unit in roster.UnitsOf(company, season).Where(unit => unit.AspectOf is null && unit.Progress?.HasPerkPointsToSpend == true))
                {
                    Troopers.Add(unit);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Reading the company for ranking up failed: {ex.Message}");
        }

        ShowTrooper(Troopers.FirstOrDefault(unit => unit.EntryIndex == keepEntry) ?? Troopers.FirstOrDefault());
    }

    [RelayCommand]
    private void SelectTrooper(CompanyViewerUnitListItem? trooper)
    {
        if (trooper is not null)
        {
            ShowTrooper(trooper);
        }
    }

    private void ShowTrooper(CompanyViewerUnitListItem? trooper)
    {
        foreach (var unit in Troopers)
        {
            unit.IsSelected = ReferenceEquals(unit, trooper);
        }

        Trooper = trooper;
        RolledLists = [];
        RollText = string.Empty;
        Options = [];
        if (trooper?.Progress is not { NextPerkPoint: { } kind } progress)
        {
            Lists = [];
            return;
        }

        PointKind = kind;
        RankText = $"{progress.RankName} · {progress.TotalXp} XP";
        PointsText = kind == PerkPointKind.Tag
            ? $"{progress.TagPerkPointsOwed} TAG perk{(progress.TagPerkPointsOwed == 1 ? string.Empty : "s")} to take from the Mecha tree"
            : $"{progress.PerkPointsOwed} perk point{(progress.PerkPointsOwed == 1 ? string.Empty : "s")} to spend";
        _owned = TrooperPerks.Owned(trooper, progress);
        Mode = RankUpMode.Choose;
        Lists = PerkChoices.ListsFor(kind).Select(list => new PerkListChoice(list.ListId, list.ListName)).ToList();
        SelectedList = Lists.FirstOrDefault();
    }

    partial void OnSelectedListChanged(PerkListChoice? value)
    {
        if (IsChoosing)
        {
            Options = value is null || Trooper?.Progress is not { } progress
                ? []
                : PerkChoices.Selectable(value.ListId, progress.RankLevel, _owned, progress.IsTag).Select(pick => new PerkOption(pick)).ToList();
        }
    }

    [RelayCommand]
    private void Choose()
    {
        Mode = RankUpMode.Choose;
        RolledLists = [];
        RollText = string.Empty;
        OnSelectedListChanged(SelectedList);
    }

    /// <summary>Rolls for the list; overlapping lists are offered to pick from, a single one rolls its track straight away.</summary>
    [RelayCommand]
    private void RollList()
    {
        if (!CanRoll)
        {
            return;
        }

        Mode = RankUpMode.Roll;
        Options = [];
        var roll = dice.RollD20();
        var lists = PerkChoices.ListsForRoll(roll).Select(list => new PerkListChoice(list.Id, list.Name)).ToList();
        RollText = $"List roll {roll}: {string.Join(" or ", lists.Select(list => list.Name))}";
        RolledLists = lists.Count > 1 ? lists : [];
        if (lists.Count == 1)
        {
            RollTrack(lists[0]);
        }
    }

    /// <summary>Rolls the track on <paramref name="list"/>, rolling again until the roll offers a perk the trooper can take.</summary>
    [RelayCommand]
    private void RollTrack(PerkListChoice? list)
    {
        if (list is null || Trooper?.Progress is not { } progress)
        {
            return;
        }

        RolledLists = [];
        for (var attempt = 0; attempt < MaxTrackRolls; attempt++)
        {
            var roll = dice.RollD20();
            var picks = PerkChoices.ForTrackRoll(list.ListId, roll, progress.RankLevel, _owned, progress.IsTag);
            if (picks.Count > 0)
            {
                RollText = $"{list.Name}: track roll {roll}";
                Options = picks.Select(pick => new PerkOption(pick)).ToList();
                return;
            }
        }

        RollText = $"Nothing on {list.Name} can be rolled — choose a perk instead.";
        Options = [];
    }

    /// <summary>Gives the trooper the perk and moves on to the next point.</summary>
    [RelayCommand]
    private async Task TakePerkAsync(PerkOption? option)
    {
        if (option is null || Trooper is not { } trooper)
        {
            return;
        }

        var slot = PerkSlots.SlotOf(option.Pick.Name);
        var gear = (await seasonFiles.LoadAsync(_seasonFilePath))?.UnitGear.GetValueOrDefault(trooper.EntryIndex) ?? [];
        var overridden = slot is { } filled ? UnitLoadout.From(gear)[filled] : null;
        if (slot is { } gearSlot && overridden is not null &&
            !await dialogs.ConfirmAsync(
                "Override Gear",
                $"{option.Pick.Name} fills the {gearSlot.Key()} slot, so {overridden} comes off and is lost, with no CR back.",
                "Take Perk",
                "Cancel"))
        {
            return;
        }

        await seasonFiles.UpdateAsync(_seasonFilePath, season =>
        {
            season.TrooperFor(trooper.EntryIndex).Perks.Add(option.Pick.NodeId);
            if (slot is { } lostSlot && overridden is not null && season.UnitGear.GetValueOrDefault(trooper.EntryIndex) is { } saved)
            {
                var loadout = UnitLoadout.From(saved);
                loadout[lostSlot] = null;
                season.UnitGear[trooper.EntryIndex] = loadout.ToSeasonGear();
                SeasonRounds.CurrentMarketplace(season).Transactions.Add(new SeasonTransaction
                {
                    OriginStore = PerkSlots.LostToPerk,
                    ItemName = overridden,
                    IsSale = true
                });
            }
        });
        await ReloadAsync(trooper.EntryIndex);
    }

    [RelayCommand]
    private Task BackToBaseAsync() => NavigationService.PopToRootAsync();
}
