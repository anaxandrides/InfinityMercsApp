using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class UnitListViewModelTests
{
    [Fact]
    public void Apply_Criteria_TakesItsTogglesAndShowsAsActive()
    {
        var list = new UnitListViewModel(waitsForTeamEntries: false);

        list.Apply(new UnitFilterCriteria { LieutenantOnlyUnits = true, TeamsView = true });

        Assert.True(list.LieutenantOnlyUnits);
        Assert.True(list.TeamsView);
        Assert.True(list.IsFilterActive);
        Assert.True(list.ShowTeamsList);
        Assert.False(list.ShowUnitsList);
    }

    [Fact]
    public void ShowTeamsList_WaitingForEntries_ShowsTheUnitsUntilReady()
    {
        var list = new UnitListViewModel(waitsForTeamEntries: true) { TeamsView = true };

        list.Clear();

        Assert.True(list.ShowUnitsList);
        Assert.False(list.ShowTeamsList);

        list.AreTeamEntriesReady = true;
        Assert.True(list.ShowTeamsList);
    }

    [Fact]
    public void DropLieutenantsOnly_LieutenantFilter_KeepsTheRestOfTheFilter()
    {
        var list = new UnitListViewModel(waitsForTeamEntries: false);
        list.Apply(new UnitFilterCriteria { LieutenantOnlyUnits = true, MinPoints = 10 });

        list.DropLieutenantsOnly();

        Assert.False(list.LieutenantOnlyUnits);
        Assert.Equal(10, list.ActiveFilter.MinPoints);
    }

    [Fact]
    public void ShowAllWhenNoneVisible_EveryUnitHidden_ShowsThemAll()
    {
        var list = new UnitListViewModel(waitsForTeamEntries: false);
        list.Units.Add(new ArmyUnitSelectionItem { Id = 1, Name = "Fusilier", IsVisible = false });
        list.Units.Add(new ArmyUnitSelectionItem { Id = 2, Name = "Bolt", IsVisible = false });

        list.ShowAllWhenNoneVisible();

        Assert.All(list.Units, unit => Assert.True(unit.IsVisible));
    }

    [Fact]
    public void RefreshTeamVisibility_NoUnitShown_HidesTheFireteam()
    {
        var list = new UnitListViewModel(waitsForTeamEntries: false);
        list.Units.Add(new ArmyUnitSelectionItem { Id = 1, Name = "Fusilier", IsVisible = false });
        list.TeamEntries.Add(new ArmyTeamListItem
        {
            Name = "Fusilier Core",
            AllowedProfiles = [new ArmyTeamUnitLimitItem { Name = "Fusilier", ResolvedUnitId = 1 }]
        });

        list.RefreshTeamVisibility();

        Assert.False(list.TeamEntries[0].IsVisible);
    }
}
