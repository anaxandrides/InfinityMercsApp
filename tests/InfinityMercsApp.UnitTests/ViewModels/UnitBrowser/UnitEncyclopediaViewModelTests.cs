using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.UnitBrowser;
using NSubstitute;
using InfinityMercsApp.ViewModels.Layouts;
using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.UnitTests.ViewModels.UnitBrowser;

public sealed class UnitEncyclopediaViewModelTests
{
    private readonly IArmyDataService _data = Army101Data.Create();

    private async Task<UnitEncyclopediaViewModel> OpenAsync()
    {
        var viewModel = new UnitEncyclopediaViewModel(Substitute.For<INavigationService>(), _data, new UnitDetailViewModel(_data));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task ListsFactionsAndFiltersToSectorials()
    {
        var viewModel = await OpenAsync();
        Assert.Equal(["PanOceania", "Military Orders"], viewModel.Factions.Select(f => f.Name));

        viewModel.ShowSectorialEntriesOnly = true;
        Assert.Equal(["Military Orders"], viewModel.Factions.Select(f => f.Name));
        Assert.False(viewModel.ShowAllFactionEntries);

        viewModel.ShowFactionEntriesOnly = true;
        Assert.Equal(["PanOceania"], viewModel.Factions.Select(f => f.Name));
    }

    [Fact]
    public async Task BrowsingStepsFromFactionToUnitToDetailAndBack()
    {
        var viewModel = await OpenAsync();
        Assert.Equal(BrowseStep.Factions, viewModel.Step);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);

        Assert.Equal(BrowseStep.Units, viewModel.Step);
        Assert.True(viewModel.Factions[0].IsSelected);
        Assert.NotEmpty(viewModel.Units);
        Assert.Equal($"{viewModel.Units.Count} units loaded.", viewModel.UnitsStatus);
        Assert.NotEqual("-", viewModel.FireteamCoreCount);

        var unit = viewModel.Units.First(u => u.Name == "SWISS GUARD");
        viewModel.SelectUnitCommand.Execute(unit);

        Assert.Equal(BrowseStep.Detail, viewModel.Step);
        Assert.Equal("SWISS GUARD", viewModel.Detail.UnitNameHeading);
        Assert.NotEmpty(viewModel.Detail.Profiles);

        viewModel.BackToUnitsCommand.Execute(null);
        Assert.Equal(BrowseStep.Units, viewModel.Step);
        viewModel.BackToFactionsCommand.Execute(null);
        Assert.Equal(BrowseStep.Factions, viewModel.Step);
    }

    [Fact]
    public async Task ChangingFactionClearsTheUnit()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        viewModel.SelectUnitCommand.Execute(viewModel.Units[0]);

        viewModel.ShowSectorialEntriesOnly = true;

        Assert.Null(viewModel.SelectedFaction);
        Assert.Null(viewModel.SelectedUnit);
        Assert.Empty(viewModel.Detail.Profiles);
        Assert.Equal(BrowseStep.Factions, viewModel.Step);
    }

    [Fact]
    public async Task TheLieutenantFilterKeepsOnlyUnitsThatCanBeLieutenant()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        var allUnits = viewModel.Units.Count;

        await viewModel.ApplyUnitFilterCommand.ExecuteAsync(new UnitFilterCriteria { LieutenantOnlyUnits = true });

        Assert.True(viewModel.LieutenantOnlyUnits);
        Assert.InRange(viewModel.Units.Count, 1, allUnits - 1);
    }

    [Fact]
    public async Task FilterOptionsComeFromTheFactionsUnits()
    {
        var viewModel = await OpenAsync();
        Assert.Empty((viewModel.BuildUnitFilterPopupOptions()).Weapons);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        var options = viewModel.BuildUnitFilterPopupOptions();

        Assert.NotEmpty(options.Classification);
        Assert.NotEmpty(options.Weapons);
        Assert.True(options.MaxPoints >= 200);
    }

    [Fact]
    public async Task TabsSwitchBetweenUnitsAndFireteams()
    {
        var viewModel = await OpenAsync();
        viewModel.ShowFireteamsCommand.Execute(null);
        Assert.False(viewModel.ShowUnitsTab);
        viewModel.ShowUnitsCommand.Execute(null);
        Assert.True(viewModel.ShowUnitsTab);
    }

    [Fact]
    public async Task OpensTheFilterOnTheAppliedFilter()
    {
        var viewModel = await OpenAsync();
        Assert.False(viewModel.IsUnitFilterOpen);

        viewModel.OpenUnitFilterCommand.Execute(null);

        Assert.True(viewModel.IsUnitFilterOpen);
        var request = viewModel.UnitFilterRequest;
        Assert.NotNull(request);
        Assert.Same(viewModel.ActiveUnitFilter, request.ExistingCriteria);
        Assert.False(request.TeamsView);

        viewModel.CloseUnitFilterCommand.Execute(null);
        Assert.False(viewModel.IsUnitFilterOpen);
    }

    [Fact]
    public async Task ApplyingAFilterClosesThePopup()
    {
        var viewModel = await OpenAsync();
        viewModel.OpenUnitFilterCommand.Execute(null);

        await viewModel.ApplyUnitFilterCommand.ExecuteAsync(UnitFilterCriteria.None);

        Assert.Null(viewModel.UnitFilterRequest);
    }

    [Fact]
    public async Task OnAPhoneOnlyTheCurrentStepShows()
    {
        var viewModel = await OpenAsync();
        Assert.True(viewModel.IsFactionsPaneVisible);
        Assert.False(viewModel.IsBrowsePaneVisible || viewModel.IsDetailPaneVisible);

        viewModel.Step = BrowseStep.Units;
        Assert.True(viewModel.IsBrowsePaneVisible);
        Assert.False(viewModel.IsFactionsPaneVisible);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Expanded);
        Assert.True(viewModel.IsFactionsPaneVisible && viewModel.IsBrowsePaneVisible && viewModel.IsDetailPaneVisible);
    }
}
