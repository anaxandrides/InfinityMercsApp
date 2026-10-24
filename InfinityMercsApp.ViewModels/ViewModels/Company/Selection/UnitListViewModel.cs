using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.Components.UnitFilter;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// The units pane: the source factions' units or their fireteams, and the filter that decides which show.
/// </summary>
/// <param name="waitsForTeamEntries">Whether the fireteam list waits for its entries, so it never flashes empty.</param>
public sealed partial class UnitListViewModel(bool waitsForTeamEntries) : ObservableObject
{
    /// <summary>True while the units are being loaded.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnitsList), nameof(ShowTeamsList))]
    public partial bool TeamsView { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnitsList), nameof(ShowTeamsList))]
    public partial bool AreTeamEntriesReady { get; set; } = true;

    [ObservableProperty]
    public partial UnitFilterCriteria ActiveFilter { get; set; } = UnitFilterCriteria.None;

    [ObservableProperty]
    public partial bool LieutenantOnlyUnits { get; set; }

    /// <summary>Whether the filter button shows as active.</summary>
    [ObservableProperty]
    public partial bool IsFilterActive { get; set; }

    /// <summary>The open filter popup, or null when it is shut.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterOpen))]
    public partial UnitFilterViewModel? Filter { get; set; }

    public ObservableCollection<ArmyUnitSelectionItem> Units { get; } = [];

    public ObservableCollection<ArmyTeamListItem> TeamEntries { get; } = [];

    public bool ShowUnitsList => !TeamsView || (waitsForTeamEntries && !AreTeamEntriesReady);

    public bool ShowTeamsList => TeamsView && (!waitsForTeamEntries || AreTeamEntriesReady);

    public bool IsFilterOpen => Filter is not null;

    /// <summary>The filter's choices, prepared from the source factions' units; null until they are.</summary>
    public UnitFilterPopupOptions? PreparedOptions { get; set; }

    /// <summary>The first unit the filter shows.</summary>
    public ArmyUnitSelectionItem? FirstVisible => Units.FirstOrDefault(unit => unit.IsVisible);

    /// <summary>Empties the lists before they reload.</summary>
    public void Clear()
    {
        PreparedOptions = null;
        AreTeamEntriesReady = false;
        Units.Clear();
        TeamEntries.Clear();
    }

    /// <summary>Takes the filter applied in the popup; the lieutenant and fireteam toggles come with it.</summary>
    public void Apply(UnitFilterCriteria criteria)
    {
        ActiveFilter = criteria;
        LieutenantOnlyUnits = criteria.LieutenantOnlyUnits;
        TeamsView = criteria.TeamsView;
        IsFilterActive = criteria.IsActive;
    }

    /// <summary>A filter of lieutenants only, or none.</summary>
    public void ShowLieutenantsOnly(bool lieutenantsOnly) => Apply(new UnitFilterCriteria { LieutenantOnlyUnits = lieutenantsOnly });

    /// <summary>Drops "lieutenants only" (and the fireteam view) from the filter.</summary>
    public void DropLieutenantsOnly()
    {
        ActiveFilter = ActiveFilter with { LieutenantOnlyUnits = false, TeamsView = false };
        LieutenantOnlyUnits = false;
        IsFilterActive = ActiveFilter.IsActive;
    }

    /// <summary>Shows every unit when the filter left none, rather than an empty list.</summary>
    public void ShowAllWhenNoneVisible()
    {
        if (Units.Any(unit => unit.IsVisible))
        {
            return;
        }

        foreach (var unit in Units)
        {
            unit.IsVisible = true;
        }
    }

    /// <summary>A fireteam shows while any of its slots does; a slot shows while a unit it stands for does.</summary>
    public void RefreshTeamVisibility()
    {
        var visible = VisibleUnits.Of(Units);
        foreach (var team in TeamEntries)
        {
            foreach (var slot in team.AllowedProfiles)
            {
                slot.IsVisible = visible.Shows(slot, team.IsWildcardBucket);
            }

            team.IsVisible = team.AllowedProfiles.Any(slot => slot.IsVisible);
            team.IsExpanded = true;
        }
    }
}
