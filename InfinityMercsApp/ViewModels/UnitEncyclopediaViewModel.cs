using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels;

/// <summary>Which factions the list shows.</summary>
public enum FactionListFilter
{
    All,
    Factions,
    Sectorials
}

/// <summary>On phones the browser shows one step at a time.</summary>
public enum BrowseStep
{
    Factions,
    Units,
    Detail
}

/// <summary>
/// Browses every faction's units and fireteams; choosing a unit shows it in <see cref="Detail"/>.
/// Used by the Unit Encyclopedia and the Perk Tester.
/// </summary>
public partial class UnitEncyclopediaViewModel(
    INavigationService navigationService,
    IArmyDataService armyData,
    UnitDetailViewModel detail,
    FactionLogoCacheService? logoCache = null) : ViewModelBase(navigationService)
{
    private List<ViewerFactionItem> _allFactions = [];

    // Set while a filter is applied, so changing its lieutenant flag does not load the units twice.
    private bool _applyingFilter;

    public UnitDetailViewModel Detail { get; } = detail;

    public ObservableCollection<ViewerFactionItem> Factions { get; } = [];
    public ObservableCollection<ViewerUnitItem> Units { get; } = [];
    public ObservableCollection<FireteamTeamItem> Fireteams { get; } = [];

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _status = "Loading factions...";
    [ObservableProperty] private string _unitsStatus = "Select a faction.";
    [ObservableProperty] private string _fireteamDuoCount = "-";
    [ObservableProperty] private string _fireteamHarisCount = "-";
    [ObservableProperty] private string _fireteamCoreCount = "-";
    [ObservableProperty] private string _fireteamsStatus = "Select a faction.";
    [ObservableProperty] private BrowseStep _step = BrowseStep.Factions;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFireteamsTab))]
    private bool _showUnitsTab = true;

    public bool ShowFireteamsTab => !ShowUnitsTab;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAllFactionEntries), nameof(ShowFactionEntriesOnly), nameof(ShowSectorialEntriesOnly))]
    private FactionListFilter _factionFilter = FactionListFilter.All;

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

    [ObservableProperty] private bool _mercsOnlyUnits;
    [ObservableProperty] private bool _lieutenantOnlyUnits;
    [ObservableProperty] private UnitFilterCriteria _activeUnitFilter = UnitFilterCriteria.None;
    [ObservableProperty] private ViewerFactionItem? _selectedFaction;
    [ObservableProperty] private ViewerUnitItem? _selectedUnit;

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
    private Task SelectUnitAsync(ViewerUnitItem? unit)
    {
        if (unit is null || SelectedFaction is null)
        {
            return Task.CompletedTask;
        }

        SelectedUnit = unit;
        return Detail.LoadAsync(new UnitDetailRequest(
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

    [RelayCommand]
    private Task ApplyUnitFilterAsync(UnitFilterCriteria? criteria)
    {
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
    public Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync(CancellationToken cancellationToken = default)
    {
        var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var characteristics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ammo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maxPoints = 200;

        if (SelectedFaction is { } faction)
        {
            var filters = armyData.GetFactionSnapshot(faction.Id, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty;
            var typeLookup = filters.Names(ArmyFilterSection.Type);

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

                var unitMaxCost = ViewerOptionNames.CollectAllOptionNames(
                    profileGroups,
                    filters.Names(ArmyFilterSection.Chars),
                    filters.Names(ArmyFilterSection.Skills),
                    filters.Names(ArmyFilterSection.Equip),
                    filters.Names(ArmyFilterSection.Weapons),
                    filters.Names(ArmyFilterSection.Ammunition),
                    characteristics,
                    skills,
                    equipment,
                    weapons,
                    ammo);
                maxPoints = Math.Max(maxPoints, unitMaxCost);
            }
        }

        static List<string> Sorted(HashSet<string> values) => values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        return Task.FromResult(new UnitFilterPopupOptions
        {
            Classification = Sorted(classification),
            Characteristics = Sorted(characteristics),
            Skills = Sorted(skills),
            Equipment = Sorted(equipment),
            Weapons = Sorted(weapons),
            Ammo = Sorted(ammo),
            MinPoints = 0,
            MaxPoints = SelectedFaction is null ? maxPoints : Math.Max(1, maxPoints)
        });
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
        FireteamDuoCount = fireteamChart.SpecDisplay("DUO");
        FireteamHarisCount = fireteamChart.SpecDisplay("HARIS");
        FireteamCoreCount = fireteamChart.SpecDisplay("CORE");

        var allowedSlugs = unitSlugs
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedNames = unitNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => UnitDetailText.NormalizeFireteamUnitName(x!.Trim()))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var team in UnitDetailText.FireteamTeams(chart, MercsOnlyUnits, allowedSlugs, allowedNames))
        {
            Fireteams.Add(team);
        }

        FireteamsStatus = Fireteams.Count == 0 ? "No fireteams available." : $"{Fireteams.Count} fireteams loaded.";
    }
}
