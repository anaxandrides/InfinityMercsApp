using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Fireteams;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Presentation.UnitBrowser;
using InfinityMercsApp.Presentation.Units;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.Components.UnitFilter;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.UnitBrowser;

/// <summary>
/// Browses every faction's units and fireteams; choosing a unit shows it in <see cref="Detail"/>.
/// Used by the Unit Encyclopedia and the Perk Tester.
/// </summary>
public partial class UnitEncyclopediaViewModel(
    INavigationService navigationService,
    IArmyDataService armyData,
    UnitDetailViewModel detail,
    FactionLogoCacheService? logoCache = null)
    : AdaptiveViewModelBase<UnitEncyclopediaLayout>(navigationService, UnitEncyclopediaLayout.For)
{
    private List<ViewerFactionItem> _allFactions = [];

    // Set while a filter is applied, so changing its lieutenant flag does not load the units twice.
    private bool _applyingFilter;

    public UnitDetailViewModel Detail { get; } = detail;

    public ObservableCollection<ViewerFactionItem> Factions { get; } = [];
    public ObservableCollection<ViewerUnitItem> Units { get; } = [];
    public ObservableCollection<FireteamTeamItem> Fireteams { get; } = [];

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = "Loading factions...";
    [ObservableProperty] public partial string UnitsStatus { get; set; } = "Select a faction.";
    [ObservableProperty] public partial string FireteamDuoCount { get; set; } = "-";
    [ObservableProperty] public partial string FireteamHarisCount { get; set; } = "-";
    [ObservableProperty] public partial string FireteamCoreCount { get; set; } = "-";
    [ObservableProperty] public partial string FireteamsStatus { get; set; } = "Select a faction.";
    [ObservableProperty]
    public partial BrowseStep Step { get; set; } = BrowseStep.Factions;

    [ObservableProperty]
    public partial bool IsFactionsPaneVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsBrowsePaneVisible { get; set; }

    [ObservableProperty]
    public partial bool IsDetailPaneVisible { get; set; }

    protected override void OnLayoutApplied() => ShowPanes();

    partial void OnStepChanged(BrowseStep value) => ShowPanes();

    // Which panes show for the layout and the step the user is on.
    private void ShowPanes()
    {
        IsFactionsPaneVisible = Layout.Shows(BrowseStep.Factions, Step);
        IsBrowsePaneVisible = Layout.Shows(BrowseStep.Units, Step);
        IsDetailPaneVisible = Layout.Shows(BrowseStep.Detail, Step);
    }

    [ObservableProperty]
    public partial bool ShowUnitsTab { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAllFactionEntries), nameof(ShowFactionEntriesOnly), nameof(ShowSectorialEntriesOnly))]
    public partial FactionListFilter FactionFilter { get; set; } = FactionListFilter.All;

    public bool ShowAllFactionEntries
    {
        get => FactionFilter == FactionListFilter.All;
        set { if (value) FactionFilter = FactionListFilter.All; }
    }

    public bool ShowFactionEntriesOnly
    {
        get => FactionFilter == FactionListFilter.Factions;
        set { if (value) FactionFilter = FactionListFilter.Factions; }
    }

    public bool ShowSectorialEntriesOnly
    {
        get => FactionFilter == FactionListFilter.Sectorials;
        set { if (value) FactionFilter = FactionListFilter.Sectorials; }
    }

    [ObservableProperty] public partial bool MercsOnlyUnits { get; set; }
    [ObservableProperty] public partial bool LieutenantOnlyUnits { get; set; }
    [ObservableProperty] public partial UnitFilterCriteria ActiveUnitFilter { get; set; } = UnitFilterCriteria.None;
    [ObservableProperty] public partial ViewerFactionItem? SelectedFaction { get; set; }
    [ObservableProperty] public partial ViewerUnitItem? SelectedUnit { get; set; }

    /// <summary>Loads the faction list unless it is already there.</summary>
    public override Task InitializeAsync() => Factions.Count > 0 || IsLoading ? Task.CompletedTask : LoadFactionsAsync();

    public async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        Detail.RefreshDisplayUnits();

        try
        {
            IsLoading = true;
            Status = "Loading factions...";
            var factions = armyData.GetMetadataFactions(includeDiscontinued: true, cancellationToken);
            if (logoCache is not null)
            {
                await logoCache.CacheFactionLogosFromRecordsAsync(
                    factions.Select(x => new FactionRecord
                    {
                        Id = x.Id,
                        ParentId = x.ParentId,
                        Name = x.Name,
                        Slug = x.Slug,
                        Discontinued = x.Discontinued,
                        Logo = x.Logo
                    }),
                    cancellationToken);
            }

            _allFactions = factions.Select(faction => new ViewerFactionItem
            {
                Id = faction.Id,
                ParentId = faction.ParentId,
                Name = faction.Name,
                Logo = faction.Logo,
                CachedLogoPath = logoCache?.TryGetCachedLogoPath(faction.Id),
                PackagedLogoPath = logoCache?.GetPackagedFactionLogoPath(faction.Id) ?? $"SVGCache/factions/{faction.Id}.svg"
            }).ToList();

            ApplyFactionFilter();
            Status = factions.Count == 0 ? "No factions available." : $"{factions.Count} factions loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading factions failed: {ex.Message}");
            Status = $"Failed to load factions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task SelectFactionAsync(ViewerFactionItem? faction)
    {
        if (faction is null)
        {
            return Task.CompletedTask;
        }

        SelectedFaction = faction;
        return LoadUnitsAsync();
    }

    [RelayCommand]
    private void SelectUnit(ViewerUnitItem? unit)
    {
        if (unit is null || SelectedFaction is null)
        {
            return;
        }

        SelectedUnit = unit;
        Detail.Load(new UnitDetailRequest(
            SelectedFaction.Id,
            unit.Id,
            unit.Name,
            UnitLogo.Source(unit.CachedLogoPath, unit.PackagedLogoPath),
            MercsOnlyUnits,
            LieutenantOnlyUnits));
    }

    [RelayCommand]
    private void ShowUnits() => ShowUnitsTab = true;

    [RelayCommand]
    private void ShowFireteams() => ShowUnitsTab = false;

    /// <summary>Phone back button from the unit list.</summary>
    [RelayCommand]
    private void BackToFactions() => Step = BrowseStep.Factions;

    /// <summary>Phone back button from the unit detail.</summary>
    [RelayCommand]
    private void BackToUnits() => Step = BrowseStep.Units;

    /// <summary>The open unit filter popup; null while it is closed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnitFilterOpen))]
    public partial UnitFilterViewModel? UnitFilter { get; set; }

    public bool IsUnitFilterOpen => UnitFilter is not null;

    /// <summary>Opens the filter popup on the selected faction's choices and the filter already applied.</summary>
    [RelayCommand]
    private void OpenUnitFilter()
    {
        try
        {
            UnitFilter = new UnitFilterViewModel(
                new UnitFilterRequest(BuildUnitFilterPopupOptions(), ActiveUnitFilter, LieutenantOnlyUnits),
                ApplyUnitFilterCommand,
                CloseUnitFilterCommand);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unit filter popup open failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CloseUnitFilter() => UnitFilter = null;

    /// <summary>Closes the filter popup and shows the units matching its filter.</summary>
    [RelayCommand]
    private Task ApplyUnitFilterAsync(UnitFilterCriteria? criteria)
    {
        UnitFilter = null;
        ActiveUnitFilter = criteria ?? UnitFilterCriteria.None;
        _applyingFilter = true;
        LieutenantOnlyUnits = ActiveUnitFilter.LieutenantOnlyUnits;
        _applyingFilter = false;
        return SelectedFaction is null ? Task.CompletedTask : LoadUnitsAsync();
    }

    partial void OnFactionFilterChanged(FactionListFilter value) => ApplyFactionFilter();

    partial void OnMercsOnlyUnitsChanged(bool value) => ReloadUnits();

    partial void OnLieutenantOnlyUnitsChanged(bool value) => ReloadUnits();

    partial void OnSelectedFactionChanged(ViewerFactionItem? oldValue, ViewerFactionItem? newValue)
    {
        oldValue?.IsSelected = false;
        newValue?.IsSelected = true;
        SelectedUnit = null;
        Detail.Reset();
        Step = newValue is null ? BrowseStep.Factions : BrowseStep.Units;
    }

    partial void OnSelectedUnitChanged(ViewerUnitItem? oldValue, ViewerUnitItem? newValue)
    {
        oldValue?.IsSelected = false;
        newValue?.IsSelected = true;
        if (newValue is not null)
        {
            Step = BrowseStep.Detail;
        }
    }

    // Changing the unit filters while a faction is open reloads its units.
    private void ReloadUnits()
    {
        if (SelectedFaction is not null && !_applyingFilter)
        {
            _ = LoadUnitsAsync();
        }
    }

    /// <summary>The selected faction's units (after the active filter) and fireteams.</summary>
    public async Task LoadUnitsAsync(CancellationToken cancellationToken = default)
    {
        Units.Clear();
        SelectedUnit = null;
        Detail.Reset();
        ResetFireteams();

        if (SelectedFaction is not { } faction)
        {
            UnitsStatus = "Select a faction.";
            return;
        }

        try
        {
            UnitsStatus = "Loading units...";
            var units = armyData.GetResumeByFaction(faction.Id, cancellationToken);
            var snapshot = armyData.GetFactionSnapshot(faction.Id, cancellationToken);
            ShowFireteams(snapshot?.FireteamChart, units.Select(x => x.Slug), units.Select(x => x.Name));

            var filters = snapshot?.Filters ?? ArmyFilterCatalog.Empty;
            var typeLookup = filters.Names(ArmyFilterSection.Type);
            var lookups = UnitFilterLookups.From(filters);
            var categoryLookup = filters.Names(ArmyFilterSection.Category);
            var filterLookups = UnitFilterLookups.From(filters);

            if (logoCache is not null)
            {
                UnitsStatus = "Preparing unit SVG cache...";
                await logoCache.CacheUnitLogosFromRecordsAsync(faction.Id, units, cancellationToken);
            }

            var orderedUnits = units
                .OrderBy(unit => ArmyUnitSort.GetUnitTypeSortIndex(unit.Type))
                .ThenBy(unit => unit.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var unit in orderedUnits)
            {
                if (!UnitOptionFilter.MatchesClassification(ActiveUnitFilter, unit.Type, typeLookup))
                {
                    continue;
                }

                var profileGroups = armyData.GetUnit(faction.Id, unit.UnitId, cancellationToken)?.ProfileGroups ?? UnitProfileGroups.Empty;
                if (!UnitOptionFilter.HasVisibleOption(
                        profileGroups,
                        filterLookups,
                        ActiveUnitFilter,
                        requireLieutenant: ActiveUnitFilter.LieutenantOnlyUnits,
                        requireZeroSwc: false))
                {
                    continue;
                }

                Units.Add(new ViewerUnitItem
                {
                    Id = unit.UnitId,
                    Name = unit.Name,
                    Logo = unit.Logo,
                    Subtitle = UnitDetailText.UnitSubtitle(unit.Type, unit.Category, typeLookup, categoryLookup),
                    CachedLogoPath = logoCache?.TryGetCachedUnitLogoPath(faction.Id, unit.UnitId),
                    PackagedLogoPath = logoCache?.GetPackagedUnitLogoPath(faction.Id, unit.UnitId)
                        ?? $"SVGCache/units/{faction.Id}-{unit.UnitId}.svg"
                });
            }

            UnitsStatus = Units.Count == 0 ? "No units available for this faction." : $"{Units.Count} units loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading units failed: {ex.Message}");
            UnitsStatus = $"Failed to load units: {ex.Message}";
        }
    }

    /// <summary>The choices offered by the unit filter popup, from the selected faction's units.</summary>
    public UnitFilterPopupOptions BuildUnitFilterPopupOptions(CancellationToken cancellationToken = default)
    {
        var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = UnitFilterOptionSets.IgnoringCase();
        var maxPoints = 200;

        if (SelectedFaction is { } faction)
        {
            var filters = armyData.GetFactionSnapshot(faction.Id, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty;
            var typeLookup = filters.Names(ArmyFilterSection.Type);
            var lookups = UnitFilterLookups.From(filters);

            foreach (var unit in armyData.GetResumeByFaction(faction.Id, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (unit.Type is { } type && typeLookup.TryGetValue(type, out var typeName) && !string.IsNullOrWhiteSpace(typeName))
                {
                    classification.Add(typeName.Trim());
                }

                if (armyData.GetUnit(faction.Id, unit.UnitId, cancellationToken)?.ProfileGroups is not { IsEmpty: false } profileGroups)
                {
                    continue;
                }

                var unitMaxCost = ViewerOptionNames.CollectAllOptionNames(profileGroups, lookups, names);
                maxPoints = Math.Max(maxPoints, unitMaxCost);
            }
        }

        return new UnitFilterPopupOptions
        {
            Classification = UnitFilterOptionSets.Sorted(classification),
            Characteristics = UnitFilterOptionSets.Sorted(names.Characteristics),
            Skills = UnitFilterOptionSets.Sorted(names.Skills),
            Equipment = UnitFilterOptionSets.Sorted(names.Equipment),
            Weapons = UnitFilterOptionSets.Sorted(names.Weapons),
            Ammo = UnitFilterOptionSets.Sorted(names.Ammo),
            MinPoints = 0,
            MaxPoints = SelectedFaction is null ? maxPoints : Math.Max(1, maxPoints)
        };
    }

    private void ApplyFactionFilter()
    {
        var filtered = FactionFilter switch
        {
            FactionListFilter.Factions => _allFactions.Where(x => x.ParentId <= 0 || x.Id == x.ParentId),
            FactionListFilter.Sectorials => _allFactions.Where(x => x.ParentId > 0 && x.Id != x.ParentId),
            _ => _allFactions
        };

        Factions.Clear();
        foreach (var faction in filtered)
        {
            Factions.Add(faction);
        }

        if (SelectedFaction is not null && !Factions.Contains(SelectedFaction))
        {
            SelectedFaction = null;
            Units.Clear();
            UnitsStatus = "Select a faction.";
        }
    }

    private void ResetFireteams()
    {
        FireteamDuoCount = FireteamHarisCount = FireteamCoreCount = "-";
        Fireteams.Clear();
        FireteamsStatus = "No fireteams available.";
    }

    private void ShowFireteams(FireteamChart? chart, IEnumerable<string?> unitSlugs, IEnumerable<string?> unitNames)
    {
        var fireteamChart = chart ?? FireteamChart.Empty;
        FireteamDuoCount = fireteamChart.SpecDisplay(FireteamType.Duo);
        FireteamHarisCount = fireteamChart.SpecDisplay(FireteamType.Haris);
        FireteamCoreCount = fireteamChart.SpecDisplay(FireteamType.Core);

        var allowedSlugs = unitSlugs
            .OfType<string>()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedNames = unitNames
            .OfType<string>()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => UnitDetailText.NormalizeFireteamUnitName(x.Trim()))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var team in UnitDetailText.FireteamTeams(chart, MercsOnlyUnits, allowedSlugs, allowedNames))
        {
            Fireteams.Add(team);
        }

        FireteamsStatus = Fireteams.Count == 0 ? "No fireteams available." : $"{Fireteams.Count} fireteams loaded.";
    }
}
