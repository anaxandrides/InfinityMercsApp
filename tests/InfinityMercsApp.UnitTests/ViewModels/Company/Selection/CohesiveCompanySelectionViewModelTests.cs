using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class CohesiveCompanySelectionViewModelTests
{
    private readonly SelectionTestKit _kit = new();

    private Task<CohesiveCompanySelectionViewModel> OpenAsync() =>
        SelectionTestKit.OpenAsync(new CohesiveCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.Sectorials));

    private async Task<CohesiveCompanySelectionViewModel> OpenWithFactionAsync()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        return viewModel;
    }

    [Fact]
    public async Task SectorialsThatCanFieldACoreFireteamAreOfferedAndTheAnswerIsCached()
    {
        var viewModel = await OpenAsync();

        Assert.Equal("Cohesive Company", viewModel.CompanyTypeLabel);
        Assert.Equal("One Core fireteam per company", viewModel.FireteamRuleText);
        Assert.True(viewModel.ShowsFireteamTracking);
        Assert.False(viewModel.ShowRightSlot);
        Assert.Equal([SelectionTestKit.Sectorial], viewModel.Factions.Select(faction => faction.Id));
        _kit.SpecOps.Received(1).UpsertCCFactionFireteamValidity(
            SelectionTestKit.Sectorial, Arg.Is<string>(key => key.Contains("pts:75")), true, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TheUnitsPaneOpensOnFireteams()
    {
        var viewModel = await OpenWithFactionAsync();

        Assert.True(viewModel.TeamsView);
        Assert.True(viewModel.ShowTeamsList);
        Assert.False(viewModel.ShowUnitsList);
        Assert.NotEmpty(viewModel.TeamEntries);
        Assert.All(viewModel.TeamEntries.Where(team => !team.IsWildcardBucket), team => Assert.StartsWith("C: ", team.TeamCountsText));
        Assert.All(viewModel.TeamEntries.SelectMany(team => team.AllowedProfiles), slot => Assert.Equal("0", slot.Min));
    }

    [Fact]
    public async Task AFireteamIsTrackedByDefaultAndCanBeChanged()
    {
        var viewModel = await OpenWithFactionAsync();
        var trackable = viewModel.TeamEntries.Where(team => team.ShowTrackingRadioButton).ToList();

        Assert.Equal(trackable[0].Name, viewModel.TrackedFireteamName);
        Assert.True(trackable[0].IsTrackedTeam);

        if (trackable.Count > 1)
        {
            trackable[1].IsTrackedTeam = true;
            Assert.Equal(trackable[1].Name, viewModel.TrackedFireteamName);
            Assert.False(trackable[0].IsTrackedTeam);
        }
    }

    [Fact]
    public async Task NoTrackedFireteamReadsAsSelectFireteam()
    {
        var viewModel = await OpenWithFactionAsync();

        viewModel.SetTrackedFireteam(null);

        Assert.Equal("Select fireteam", viewModel.TrackedFireteamNameDisplay);
        Assert.Equal(0, viewModel.TrackedFireteamLevel);
        Assert.Null(viewModel.TrackedFireteamLevelIcon);
    }

    [Fact]
    public async Task TroopersJoinWithNumberTwoAndTheLieutenantWithFireteamMaster()
    {
        var viewModel = await OpenWithFactionAsync();
        viewModel.TeamsView = false;
        viewModel.ApplyUnitFilterCommand.Execute(new InfinityMercsApp.Domain.Models.Filtering.UnitFilterCriteria { LieutenantOnlyUnits = true });
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.Units.First(unit => unit.IsVisible));
        var lieutenant = viewModel.Profiles.First(profile => profile.IsLieutenant && profile.IsVisible);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(lieutenant);

        var entry = Assert.Single(viewModel.MercsCompanyEntries);
        Assert.Contains(CohesiveCompanySelectionViewModel.NumberTwoSkill, entry.SavedSkills);
        Assert.Contains(CohesiveCompanySelectionViewModel.FireteamMasterSkill, entry.SavedSkills);
    }

    [Fact]
    public async Task APointsLimitChangeRechecksTheSectorials()
    {
        var viewModel = await OpenWithFactionAsync();
        _kit.SpecOps.ClearReceivedCalls();

        viewModel.SelectedStartSeasonPoints = "150";
        await Task.Yield();

        _kit.SpecOps.Received().GetCCFactionFireteamValidity(Arg.Is<string>(key => key.Contains("pts:150")), Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }
}
