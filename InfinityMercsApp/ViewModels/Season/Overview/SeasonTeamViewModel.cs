using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Perks.Catalog;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Retirement;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Presentation.Season.Overview;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The season's Team tab: the company's troopers in a drop-down, and the chosen one's stats, equipment,
/// skills and weapons, with its gear pickers (<see cref="Gear"/>); an augment that sets a stat ("WIP=12") changes
/// the stat line. Any trooper but the captain can be retired, a peripheral too unless it is a Servant one.
/// </summary>
public sealed partial class SeasonTeamViewModel : ObservableObject
{
    public const string NoUnitTitle = "Select unit...";

    private readonly CompanyRoster _roster;
    private readonly IAppPackageFiles _packageFiles;
    private readonly ISeasonFileService _seasonFiles;
    private readonly MarketplaceDetailViewModel _detailPopup;
    private readonly IDialogService _dialogs;
    private readonly Func<Task> _seasonChanged;
    private readonly ITeamChanges _changes;
    private SavedCompanyFile? _company;
    private SavedImprovedCaptainStats _captain = new();
    private string? _seasonFilePath;

    public SeasonTeamViewModel(
        CompanyRoster roster,
        UnitDetailViewModel detail,
        IAppPackageFiles packageFiles,
        ISeasonFileService seasonFiles,
        MarketplaceDetailViewModel detailPopup,
        IDialogService dialogs,
        Func<Task> seasonChanged,
        ITeamChanges changes)
    {
        _dialogs = dialogs;
        _seasonChanged = seasonChanged;
        _changes = changes;
        _roster = roster;
        _packageFiles = packageFiles;
        _seasonFiles = seasonFiles;
        _detailPopup = detailPopup;
        Detail = detail;
        Gear = new TeamGearViewModel(seasonFiles, dialogs, changes, seasonChanged, Units);
    }

    /// <summary>The selected trooper's stat line, icons and logo.</summary>
    public UnitDetailViewModel Detail { get; }

    public ObservableCollection<CompanyViewerUnitListItem> Units { get; } = [];

    /// <summary>The selected trooper's gear pickers.</summary>
    public TeamGearViewModel Gear { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PickerTitle))]
    public partial CompanyViewerUnitListItem? SelectedUnit { get; set; }

    [ObservableProperty]
    public partial bool IsUnitPickerOpen { get; set; }

    [ObservableProperty]
    public partial string SelectedName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedBaseName))]
    public partial string SelectedBaseName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedUnitType))]
    public partial string SelectedUnitType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ProfileColumns Equipment { get; set; } = ProfileColumns.Empty;

    [ObservableProperty]
    public partial ProfileColumns Skills { get; set; } = ProfileColumns.Empty;

    [ObservableProperty]
    public partial ProfileColumns RangedWeapons { get; set; } = ProfileColumns.Empty;

    [ObservableProperty]
    public partial ProfileColumns MeleeWeapons { get; set; } = ProfileColumns.Empty;

    /// <summary>The selected trooper's rank and XP, e.g. "Hired Gun · 18 XP".</summary>
    [ObservableProperty]
    public partial string RankText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPerks))]
    public partial IReadOnlyList<string> Perks { get; set; } = [];

    /// <summary>The selected trooper's injuries with their effects, e.g. "Arms: -1 BS".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInjuries))]
    public partial IReadOnlyList<string> Injuries { get; set; } = [];

    [ObservableProperty]
    public partial bool HasPerkPointsToSpend { get; set; }

    /// <summary>Whether the selected trooper can retire: anyone but the captain (a peripheral leaves with its trooper).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RetireCommand))]
    public partial bool CanRetireSelected { get; set; }

    /// <summary>The roll button's text for a trooper still owed a Booty or Metachemistry roll, e.g. "ROLL BOOTY".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOwedRoll))]
    public partial string OwedRollText { get; set; } = string.Empty;

    public bool HasOwedRoll => OwedRollText.Length > 0;

    /// <summary>Whether the selected trooper is a peripheral no one controls, which can be sold.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SellPeripheralCommand))]
    public partial bool CanSellSelected { get; set; }

    /// <summary>The sell button's text, e.g. "SELL PERIPHERAL · 2 CR".</summary>
    [ObservableProperty]
    public partial string SellText { get; set; } = string.Empty;

    public bool HasPerks => Perks.Count > 0;

    public bool HasInjuries => Injuries.Count > 0;

    /// <summary>The drop-down's heading: the selected trooper's name.</summary>
    public string PickerTitle => SelectedUnit?.Name ?? NoUnitTitle;

    public bool HasSelectedBaseName => SelectedBaseName.Length > 0;

    public bool HasSelectedUnitType => SelectedUnitType.Length > 0;

    /// <summary>
    /// Shows a company's troopers as they stand in the season, with their season gear and the armour they wear,
    /// which <paramref name="catalog"/> describes. The trooper shown before stays selected; otherwise the first is.
    /// </summary>
    public async Task LoadAsync(SavedCompanyFile company, string? seasonFilePath, SeasonFile? season, InventoryCatalog catalog)
    {
        var shownEntry = SelectedUnit?.EntryIndex;
        _company = company;
        _captain = company.ImprovedCaptainStats ?? new SavedImprovedCaptainStats();
        _seasonFilePath = seasonFilePath;
        Gear.Load(seasonFilePath, season?.UnitGear ?? []);
        Units.Clear();
        SelectedUnit = null;
        foreach (var unit in _roster.UnitsOf(company, season, catalog))
        {
            unit.LogoSource = await CompanyRoster.LogoSourceAsync(unit, _packageFiles);
            Units.Add(unit);
        }

        if (Units.Count > 0)
        {
            SelectUnit(Units.FirstOrDefault(unit => unit.EntryIndex == shownEntry) ?? Units[0]);
        }
    }

    /// <summary>Clears the tab when there is no company to show.</summary>
    public void Clear()
    {
        Units.Clear();
        SelectedUnit = null;
        SelectedName = SelectedBaseName = SelectedUnitType = OwedRollText = string.Empty;
        CanRetireSelected = CanSellSelected = false;
    }

    /// <summary>Picks up the inches/centimetres setting, redrawing the selected trooper when it changed.</summary>
    public void RefreshDisplayUnits()
    {
        var wasInInches = Detail.ShowUnitsInInches;
        Detail.RefreshDisplayUnits();
        _detailPopup.ShowUnitsInInches = Detail.ShowUnitsInInches;
        if (wasInInches != Detail.ShowUnitsInInches)
        {
            ShowSelected();
        }
    }

    [RelayCommand]
    private void ToggleUnitPicker() => IsUnitPickerOpen = !IsUnitPickerOpen;

    [RelayCommand]
    private void CloseUnitPicker() => IsUnitPickerOpen = false;

    [RelayCommand]
    private void SelectUnit(CompanyViewerUnitListItem? item)
    {
        IsUnitPickerOpen = false;
        if (item is null)
        {
            return;
        }

        foreach (var unit in Units)
        {
            unit.IsSelected = ReferenceEquals(unit, item);
        }

        SelectedUnit = item;
        ShowSelected();
    }

    /// <summary>
    /// Retires the selected trooper once confirmed: its gear goes back to the inventory, it keeps its cost and XP
    /// as renown for the company, and it leaves the season's troopers (with any peripherals it has).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRetireSelected))]
    private async Task RetireAsync()
    {
        if (SelectedUnit is not { } item || _company is not { } company ||
            await _seasonFiles.LoadAsync(_seasonFilePath) is not { } season ||
            SeasonRetirement.For(company, item.EntryIndex, season, Gear.Catalog, _roster.ServantsOf(company)) is not { } trooper)
        {
            return;
        }

        var peripherals = trooper.Peripherals.Count == 0
            ? string.Empty
            : $" {string.Join(", ", trooper.Peripherals.Select(peripheral => peripheral.Name))} will leave with them.";
        if (!await _dialogs.ConfirmAsync(
                "Retire Trooper",
                $"Retire {trooper.Name}? Their gear goes back to the inventory, they keep {trooper.Renown} renown for the " +
                $"company, and they can't be used in later contracts.{peripherals}",
                "Retire",
                "Cancel"))
        {
            return;
        }

        if (!await _seasonFiles.UpdateAsync(_seasonFilePath, seasonFile => TrooperRetirement.Apply(seasonFile, trooper)))
        {
            await _dialogs.ShowAlertAsync("Retire Failed", $"{trooper.Name} couldn't be retired: the season file couldn't be saved.");
            return;
        }

        SelectedUnit = null;
        await _seasonChanged();
    }

    /// <summary>Rolls what the selected trooper still owes on the Booty or Metachemistry table.</summary>
    [RelayCommand]
    private Task RollOwedAsync() => SelectedUnit is { OwedRolls.Count: > 0 } item ? _changes.RollOwedAsync(item.EntryIndex) : Task.CompletedTask;

    /// <summary>Sells the selected peripheral, one from the Peripheral (Controller) augment that no trooper controls.</summary>
    [RelayCommand(CanExecute = nameof(CanSellSelected))]
    private Task SellPeripheralAsync() =>
        SelectedUnit is { IsUncontrolledPeripheral: true } item ? _changes.SellPeripheralAsync(item.EntryIndex) : Task.CompletedTask;

    [RelayCommand]
    private Task ShowWeaponAsync(ProfileLine? line) =>
        line is null || line.IsPlaceholder ? Task.CompletedTask : _detailPopup.ShowWeaponAsync(line.Text);

    private void ShowSelected()
    {
        if (SelectedUnit is not { } item)
        {
            return;
        }

        var profile = CompanyRoster.ProfileOf(item, Detail.ShowUnitsInInches);
        CompanyRoster.ShowOn(Detail, item, profile);
        var carried = Gear.CarriedBy(item.EntryIndex);
        foreach (var augment in carried.Where(gear => gear.Slot == GearSlot.Augments))
        {
            if (StatOverride.TryParse(augment.Name, out var statOverride))
            {
                Detail.ApplyAugmentStatOverride(statOverride);
            }
        }

        Detail.LogoSource = item.LogoSource;

        SelectedName = item.Name;
        var baseName = string.IsNullOrWhiteSpace(item.BaseUnitName) ? profile.Name : item.BaseUnitName;
        SelectedBaseName = string.Equals(baseName.Trim(), item.Name.Trim(), StringComparison.OrdinalIgnoreCase) ? string.Empty : baseName;
        SelectedUnitType = item.UnitTypeCode;
        CanRetireSelected = !item.IsLieutenant && item.AspectOf is null && (!item.IsPeripheralUnit || item.IsPeripheralTrooper);
        CanSellSelected = item.IsUncontrolledPeripheral;
        SellText = item.IsUncontrolledPeripheral ? $"SELL PERIPHERAL · {item.PeripheralSellPrice} CR" : string.Empty;
        ShowProgress(item);

        Detail.HeaderColors = _roster.HeaderColorsFor(item.VisualFactionId > 0 ? item.VisualFactionId : item.SourceFactionId);

        Equipment = ProfileColumns.From(ProfileText.Lines(profile.UniqueEquipment));
        Skills = ProfileColumns.From(SkillLines(profile, carried));
        ShowWeapons(profile, carried);
        Gear.Show(item);
    }

    // The captain's (or a TAG company TAG's) Spec-Ops buys are listed first among the perks, as on an army sheet, then Booty and
    // Metachemistry results.
    private void ShowProgress(CompanyViewerUnitListItem item)
    {
        var progress = item.Progress;
        RankText = progress is null ? string.Empty : $"{progress.RankName} · {progress.TotalXp} XP";
        Perks =
        [
            .. item.CarriesCaptainUpgrades ? _captain.SpecOpsPurchases(StatKeys.VitalityFor(item.UnitTypeCode)) : [],
            .. item.SpecOpsPurchases,
            .. item.RollLines,
            .. progress?.Perks.Select(perk => CompanyPerkCatalog.FindById(perk)?.Description ?? perk) ?? []
        ];
        OwedRollText = item.OwedRolls.Count == 0 ? string.Empty : $"ROLL {item.OwedRolls[0].Table.ToString().ToUpperInvariant()}";
        Injuries = [
            .. progress?.Record.Injuries.Select(injury => $"{injury.Label()}: {injury.EffectText()}") ?? []
        ];
        HasPerkPointsToSpend = progress?.HasPerkPointsToSpend == true;
    }

    // The lieutenant order first, then the rest, then any augment that is a skill rather than a stat.
    private static List<string> SkillLines(ViewerProfileItem profile, IReadOnlyList<EquippedItem> carried)
    {
        var skills = ProfileText.Lines(profile.UniqueSkills);
        LieutenantRules.MoveLieutenantFirst(skills);
        foreach (var augment in carried.Where(gear => gear.Slot == GearSlot.Augments).Select(gear => gear.Name))
        {
            if (!StatOverride.TryParse(augment, out _) && !skills.Contains(augment, StringComparer.OrdinalIgnoreCase))
            {
                skills.Add(augment);
            }
        }

        return skills;
    }

    // The profile's weapons, plus any carried as a weapon, each under ranged or melee.
    private void ShowWeapons(ViewerProfileItem profile, IReadOnlyList<EquippedItem> carried)
    {
        var ranged = ProfileText.Lines(profile.RangedWeapons);
        var melee = ProfileText.Lines(profile.MeleeWeapons);
        foreach (var weapon in carried.Where(gear => gear.Slot.HoldsWeapon()).Select(gear => gear.Name))
        {
            var list = CompanyProfileText.IsMeleeWeaponName(weapon) ? melee : ranged;
            if (!list.Contains(weapon, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(weapon);
            }
        }

        RangedWeapons = ProfileColumns.From(ranged);
        MeleeWeapons = ProfileColumns.From(melee);
    }
}
