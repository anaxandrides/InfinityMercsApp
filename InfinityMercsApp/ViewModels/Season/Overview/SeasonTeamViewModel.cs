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
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using InfinityMercsApp.ViewModels.Season.Overview.Items;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The season's Team tab: the company's troopers in a drop-down, and the chosen one's stats, equipment,
/// skills and weapons, with a picker per gear slot for the season gear the company owns. Gear choices are
/// saved to the season file; an augment that sets a stat ("WIP=12") changes the stat line. The SWC of the gear
/// equipped across the company may not go over the company's SWC. Any trooper but the captain can be retired.
/// </summary>
public partial class SeasonTeamViewModel : ObservableObject
{
    public const string NoUnitTitle = "Select unit...";

    private readonly CompanyRoster _roster;
    private readonly IAppPackageFiles _packageFiles;
    private readonly ISeasonFileService _seasonFiles;
    private readonly MarketplaceDetailViewModel _detailPopup;
    private readonly IDialogService _dialogs;
    private readonly Func<Task> _seasonChanged;
    private SavedCompanyFile? _company;
    private CompanyLoadouts _loadouts = new();
    private SeasonInventory _inventory = SeasonInventory.Empty;
    private InventoryCatalog _catalog = InventoryCatalog.Empty;
    private double _companySwc;
    private string? _seasonFilePath;

    public SeasonTeamViewModel(
        CompanyRoster roster,
        UnitDetailViewModel detail,
        IAppPackageFiles packageFiles,
        ISeasonFileService seasonFiles,
        MarketplaceDetailViewModel detailPopup,
        IDialogService dialogs,
        Func<Task> seasonChanged)
    {
        _dialogs = dialogs;
        _seasonChanged = seasonChanged;
        _roster = roster;
        _packageFiles = packageFiles;
        _seasonFiles = seasonFiles;
        _detailPopup = detailPopup;
        Detail = detail;
        GearSlots = Enum.GetValues<GearSlot>().Select(slot => new GearSlotPicker(slot, OnGearPicked)).ToList();
    }

    /// <summary>The selected trooper's stat line, icons and logo.</summary>
    public UnitDetailViewModel Detail { get; }

    public ObservableCollection<CompanyViewerUnitListItem> Units { get; } = [];

    /// <summary>A picker for each gear slot, in slot order.</summary>
    public IReadOnlyList<GearSlotPicker> GearSlots { get; }

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

    /// <summary>The company's equipped SWC against its SWC, e.g. "SWC EQUIPPED 1.5 / 2".</summary>
    [ObservableProperty]
    public partial string SwcText { get; set; } = string.Empty;

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
        _seasonFilePath = seasonFilePath;
        _loadouts = CompanyLoadouts.From(season?.UnitGear ?? []);
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
        SelectedName = SelectedBaseName = SelectedUnitType = string.Empty;
        CanRetireSelected = false;
    }

    /// <summary>
    /// Takes in what the company now owns for the gear pickers, with the catalog that knows each item's SWC and
    /// the company's SWC that equipped gear must stay within.
    /// </summary>
    public void ShowOwnedGear(SeasonInventory inventory, InventoryCatalog catalog, double companySwc)
    {
        _inventory = inventory;
        _catalog = catalog;
        _companySwc = companySwc;
        RefreshGearPickers();
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
            SeasonRetirement.For(company, item.EntryIndex, season, _catalog) is not { } trooper)
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
        var carried = EquippedItem.In(_loadouts.For(item.EntryIndex).ToSeasonGear(), _catalog);
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
        CanRetireSelected = !item.IsLieutenant && !item.IsPeripheralUnit;
        ShowProgress(item.Progress);

        Detail.HeaderColors = _roster.HeaderColorsFor(item.VisualFactionId > 0 ? item.VisualFactionId : item.SourceFactionId);

        Equipment = ProfileColumns.From(ProfileText.Lines(profile.UniqueEquipment));
        Skills = ProfileColumns.From(SkillLines(profile, carried));
        ShowWeapons(profile, carried);
        RefreshGearPickers();
    }

    private void ShowProgress(TrooperProgress? progress)
    {
        RankText = progress is null ? string.Empty : $"{progress.RankName} · {progress.TotalXp} XP";
        Perks = progress?.Perks
            .Select(perk => CompanyPerkCatalog.FindById(perk)?.Description ?? perk)
            .ToList() ?? [];
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

    // Each slot offers nothing or an owned item that fits it, noting who else carries it.
    private void RefreshGearPickers()
    {
        if (SelectedUnit is not { } item)
        {
            return;
        }

        SwcText = $"SWC EQUIPPED {SeasonEconomy.SwcText((decimal)EquippedSwc.Of(_loadouts, _catalog))} / {SeasonEconomy.SwcText((decimal)_companySwc)}";
        var loadout = _loadouts.For(item.EntryIndex);
        foreach (var picker in GearSlots)
        {
            picker.IsAvailable = picker.Slot != GearSlot.Extra || HasExtraSlot(loadout);
            List<GearOption> options = [GearOption.None];
            var selected = GearOption.None;
            foreach (var itemName in _inventory.ItemsFor(picker.Slot))
            {
                var itemOptions = OptionsFor(itemName, picker.Slot, item.EntryIndex);
                options.AddRange(itemOptions);
                if (string.Equals(itemName, loadout[picker.Slot], StringComparison.OrdinalIgnoreCase))
                {
                    selected = itemOptions[0];
                }
            }

            picker.Show(options, selected);
        }
    }

    private void OnGearPicked(GearSlot slot, GearOption option)
    {
        if (SelectedUnit is not { } item ||
            string.Equals(_loadouts.For(item.EntryIndex)[slot], option.ItemName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var equippedSwc = EquippedSwc.After(_loadouts, item.EntryIndex, slot, option.ItemName, _catalog, option.TakenFrom);
        if (equippedSwc > _companySwc && equippedSwc > EquippedSwc.Of(_loadouts, _catalog))
        {
            _ = RefuseOverSwcAsync(option, equippedSwc);
            return;
        }

        _loadouts.Assign(item.EntryIndex, slot, option.ItemName, option.TakenFrom);
        if (slot == GearSlot.Armor && !HasExtraSlot(_loadouts.For(item.EntryIndex)))
        {
            // Taking off Belts and Pouches empties its slot.
            _loadouts.Assign(item.EntryIndex, GearSlot.Extra, null);
        }

        _ = SaveThenRefreshAsync();
    }

    // A spare copy is offered as itself; with none spare, each copy another slot holds is offered, with whose it is.
    // The trooper's own copy in this slot is always offered first.
    private List<GearOption> OptionsFor(string itemName, GearSlot slot, int entryIndex)
    {
        var swc = EquippedSwc.Of(itemName, _catalog);
        var holdings = _loadouts.Holding(itemName);
        var here = new GearHolding(entryIndex, slot);
        if (holdings.Contains(here) || holdings.Count < _inventory.CountOf(itemName))
        {
            return [GearOption.For(itemName, swc, null)];
        }

        return holdings
            .Where(holding => slot.SharesItemsWith(holding.Slot))
            .Select(holding => holding.EntryIndex)
            .Distinct()
            .Select(holder => GearOption.For(itemName, swc, Units.FirstOrDefault(unit => unit.EntryIndex == holder)?.Name, takenFrom: holder))
            .ToList();
    }

    // Belts and Pouches gives one more slot.
    private bool HasExtraSlot(UnitLoadout loadout) =>
        loadout[GearSlot.Armor] is { } armor && _catalog.Find(armor)?.Armor?.Effects.ExtraSlot == true;

    // The picker goes back to what the trooper had once the alert is shown.
    private async Task RefuseOverSwcAsync(GearOption option, double equippedSwc)
    {
        await _dialogs.ShowAlertAsync(
            "Not Enough SWC",
            $"Equipping {option.ItemName} would bring the company's equipped SWC to {SeasonEconomy.SwcText((decimal)equippedSwc)}, " +
            $"over its {SeasonEconomy.SwcText((decimal)_companySwc)} SWC.");
        RefreshGearPickers();
    }

    // The season is re-read after the save, once the picker that reported the choice has finished with it: armour
    // changes the trooper's line, and gear counts toward renown.
    private async Task SaveThenRefreshAsync()
    {
        try
        {
            if (await _seasonFiles.LoadAsync(_seasonFilePath) is { } seasonFile)
            {
                seasonFile.UnitGear = _loadouts.ToSeasonGear();
                await _seasonFiles.SaveAsync(_seasonFilePath, seasonFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Saving the season gear failed: {ex.Message}");
            ShowSelected();
            return;
        }

        await _seasonChanged();
    }
}
