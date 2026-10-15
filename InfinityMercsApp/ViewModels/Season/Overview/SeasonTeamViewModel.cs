using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Perks.Catalog;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Visuals;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.ViewModels.Marketplaces;
using InfinityMercsApp.ViewModels.UnitBrowser;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The season's Team tab: the company's troopers in a drop-down, and the chosen one's stats, equipment,
/// skills and weapons, with a picker per gear slot for the season gear the company owns. Gear choices are
/// saved to the season file; an augment that sets a stat ("WIP=12") changes the stat line. The SWC of the gear
/// equipped across the company may not go over the company's SWC.
/// </summary>
public partial class SeasonTeamViewModel : ObservableObject
{
    public const string NoUnitTitle = "Select unit...";

    private readonly CompanyRoster _roster;
    private readonly IAppPackageFiles _packageFiles;
    private readonly ISeasonFileService _seasonFiles;
    private readonly MarketplaceDetailViewModel _detailPopup;
    private readonly IDialogService _dialogs;
    private SavedImprovedCaptainStats _captain = new();
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
        IDialogService dialogs)
    {
        _dialogs = dialogs;
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
    private CompanyViewerUnitListItem? _selectedUnit;

    [ObservableProperty]
    private bool _isUnitPickerOpen;

    [ObservableProperty]
    private string _selectedName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedBaseName))]
    private string _selectedBaseName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedUnitType))]
    private string _selectedUnitType = string.Empty;

    [ObservableProperty]
    private CompanyHeaderColors _headerColors = new(
        UnitDisplayConfigurationsView.DefaultHeaderPrimaryColor,
        UnitDisplayConfigurationsView.DefaultHeaderSecondaryColor,
        Colors.White,
        Colors.White);

    [ObservableProperty]
    private bool _showLieutenantIcon;

    [ObservableProperty]
    private int _lieutenantIconCount;

    [ObservableProperty]
    private bool _showPeripheralIcon;

    [ObservableProperty]
    private ProfileColumns _equipment = ProfileColumns.Empty;

    [ObservableProperty]
    private ProfileColumns _skills = ProfileColumns.Empty;

    [ObservableProperty]
    private ProfileColumns _rangedWeapons = ProfileColumns.Empty;

    [ObservableProperty]
    private ProfileColumns _meleeWeapons = ProfileColumns.Empty;

    /// <summary>The selected trooper's rank and XP, e.g. "Hired Gun · 18 XP".</summary>
    [ObservableProperty]
    private string _rankText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPerks))]
    private IReadOnlyList<string> _perks = [];

    /// <summary>The selected trooper's injuries with their effects, e.g. "Arms: -1 BS".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInjuries))]
    private IReadOnlyList<string> _injuries = [];

    [ObservableProperty]
    private bool _hasPerkPointsToSpend;

    /// <summary>The company's equipped SWC against its SWC, e.g. "SWC EQUIPPED 1.5 / 2".</summary>
    [ObservableProperty]
    private string _swcText = string.Empty;

    public bool HasPerks => Perks.Count > 0;

    public bool HasInjuries => Injuries.Count > 0;

    /// <summary>The drop-down's heading: the selected trooper's name.</summary>
    public string PickerTitle => SelectedUnit?.Name ?? NoUnitTitle;

    public bool HasSelectedBaseName => SelectedBaseName.Length > 0;

    public bool HasSelectedUnitType => SelectedUnitType.Length > 0;

    /// <summary>
    /// Shows a company's troopers as they stand in the season, with their season gear. The trooper shown before
    /// stays selected; otherwise the first is.
    /// </summary>
    public async Task LoadAsync(SavedCompanyFile company, string? seasonFilePath, SeasonFile? season)
    {
        var shownEntry = SelectedUnit?.EntryIndex;
        _captain = company.ImprovedCaptainStats ?? new SavedImprovedCaptainStats();
        _seasonFilePath = seasonFilePath;
        _loadouts = CompanyLoadouts.From(season?.UnitGear ?? []);
        Units.Clear();
        SelectedUnit = null;
        foreach (var unit in _roster.UnitsOf(company, season))
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
        CompanyRoster.ShowOn(Detail, item, profile, _captain);
        var loadout = _loadouts.For(item.EntryIndex);
        if (StatOverride.TryParse(loadout[GearSlot.Augments], out var statOverride))
        {
            Detail.ApplyAugmentStatOverride(statOverride);
        }

        Detail.LogoSource = item.LogoSource;

        SelectedName = item.Name;
        var baseName = string.IsNullOrWhiteSpace(item.BaseUnitName) ? profile.Name : item.BaseUnitName;
        SelectedBaseName = string.Equals(baseName.Trim(), item.Name.Trim(), StringComparison.OrdinalIgnoreCase) ? string.Empty : baseName;
        SelectedUnitType = item.UnitTypeCode;
        ShowProgress(item.Progress);

        HeaderColors = _roster.HeaderColorsFor(item.VisualFactionId > 0 ? item.VisualFactionId : item.SourceFactionId);
        ShowLieutenantIcon = profile.IsLieutenant;
        LieutenantIconCount = CompanyRoster.LieutenantIconCount(profile);
        ShowPeripheralIcon = UnitTraitIcons
            .From(profile.Characteristics, profile.UniqueSkills, profile.UniqueEquipment, item.IsPeripheralUnit).Peripheral;

        Equipment = ProfileColumns.From(ProfileText.Lines(profile.UniqueEquipment));
        Skills = ProfileColumns.From(SkillLines(profile, loadout));
        ShowWeapons(profile, loadout);
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

    // The lieutenant order first, then the rest, then an augment that is a skill rather than a stat.
    private static List<string> SkillLines(ViewerProfileItem profile, UnitLoadout loadout)
    {
        var skills = ProfileText.Lines(profile.UniqueSkills);
        LieutenantRules.MoveLieutenantFirst(skills);
        var augment = loadout[GearSlot.Augments];
        if (!string.IsNullOrWhiteSpace(augment) &&
            !StatOverride.TryParse(augment, out _) &&
            !skills.Contains(augment, StringComparer.OrdinalIgnoreCase))
        {
            skills.Add(augment);
        }

        return skills;
    }

    // The profile's weapons, plus any carried in a weapon slot, each under ranged or melee.
    private void ShowWeapons(ViewerProfileItem profile, UnitLoadout loadout)
    {
        var ranged = ProfileText.Lines(profile.RangedWeapons);
        var melee = ProfileText.Lines(profile.MeleeWeapons);
        foreach (var slot in Enum.GetValues<GearSlot>().Where(slot => slot.HoldsWeapon()))
        {
            if (loadout[slot] is not { } weapon)
            {
                continue;
            }

            var list = CompanyProfileTextService.IsMeleeWeaponName(weapon) ? melee : ranged;
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
            List<GearOption> options = [GearOption.None];
            var selected = GearOption.None;
            foreach (var itemName in _inventory.ItemsFor(picker.Slot))
            {
                var holder = _loadouts.HolderOf(itemName, picker.Slot, item.EntryIndex);
                var option = GearOption.For(itemName, EquippedSwc.Of(itemName, _catalog), Units.FirstOrDefault(unit => unit.EntryIndex == holder)?.Name);
                options.Add(option);
                if (string.Equals(itemName, loadout[picker.Slot], StringComparison.OrdinalIgnoreCase))
                {
                    selected = option;
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

        var equippedSwc = EquippedSwc.After(_loadouts, item.EntryIndex, slot, option.ItemName, _catalog);
        if (equippedSwc > _companySwc && equippedSwc > EquippedSwc.Of(_loadouts, _catalog))
        {
            _ = RefuseOverSwcAsync(option, equippedSwc);
            return;
        }

        _loadouts.Assign(item.EntryIndex, slot, option.ItemName);
        _ = SaveThenRefreshAsync();
    }

    // The picker goes back to what the trooper had once the alert is shown.
    private async Task RefuseOverSwcAsync(GearOption option, double equippedSwc)
    {
        await _dialogs.ShowAlertAsync(
            "Not Enough SWC",
            $"Equipping {option.ItemName} would bring the company's equipped SWC to {SeasonEconomy.SwcText((decimal)equippedSwc)}, " +
            $"over its {SeasonEconomy.SwcText((decimal)_companySwc)} SWC.");
        RefreshGearPickers();
    }

    // The pickers are refilled after the save, once the picker that reported the choice has finished with it.
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
        }

        ShowSelected();
    }
}
