using InfinityMercsApp.Domain.Fireteams;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
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

    // Whether the unit fits the tracked fireteam (or a wildcard), as the company checks it.
    private static bool CanJoinTracked(CohesiveCompanySelectionViewModel viewModel, ArmyUnitSelectionItem unit)
    {
        var slots = viewModel.TeamEntries
            .Where(team => team.IsTrackedTeam || team.IsWildcardBucket)
            .SelectMany(team => team.AllowedProfiles)
            .Select(slot => new FireteamSlot(slot.Name, slot.ResolvedUnitId, slot.ResolvedSourceFactionId, slot.Max))
            .ToList();
        return FireteamLevels.CanJoin(new FireteamMember(unit.Name, unit.Name, unit.Id, unit.SourceFactionId), slots);
    }

    private static async Task<ViewerProfileItem> SelectLieutenantAsync(CohesiveCompanySelectionViewModel viewModel, bool canJoin)
    {
        viewModel.TeamsView = false;
        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true });
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(
            viewModel.Units.First(unit => unit.IsVisible && CanJoinTracked(viewModel, unit) == canJoin));
        return viewModel.Profiles.First(profile => profile.IsLieutenant && profile.IsVisible);
    }

    [Fact]
    public async Task InitializeAsync_Sectorials_OffersThoseWithACoreFireteamAndCaches()
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
    public async Task SelectFactionCommand_Faction_OpensOnFireteams()
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
    public async Task TrackedFireteamName_UnitsLoaded_TracksOneByDefault()
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
    public async Task SetTrackedFireteam_Null_ReadsAsSelectFireteam()
    {
        var viewModel = await OpenWithFactionAsync();

        viewModel.SetTrackedFireteam(null);

        Assert.Equal("Select fireteam", viewModel.TrackedFireteamNameDisplay);
        Assert.Equal(0, viewModel.TrackedFireteamLevel);
        Assert.Null(viewModel.TrackedFireteamLevelIcon);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_Trooper_JoinsWithNumberTwoAndLieutenantFtMaster()
    {
        var viewModel = await OpenWithFactionAsync();
        var lieutenant = await SelectLieutenantAsync(viewModel, canJoin: true);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(lieutenant);

        var entry = Assert.Single(viewModel.MercsCompanyEntries);
        Assert.Contains(CohesiveCompanySelectionViewModel.NumberTwoSkill, entry.SavedSkills);
        Assert.Contains(CohesiveCompanySelectionViewModel.FireteamMasterSkill, entry.SavedSkills);
        Assert.False(entry.IrregularInCompany);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_CannotJoinTheFireteam_TurnsItAway()
    {
        var viewModel = await OpenWithFactionAsync();
        var lieutenant = await SelectLieutenantAsync(viewModel, canJoin: false);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(lieutenant);

        Assert.Empty(viewModel.MercsCompanyEntries);
        await _kit.Dialogs.Received(1).ShowAlertAsync("Can't Add Trooper", Arg.Is<string>(text => text.Contains(viewModel.TrackedFireteamName)));
    }

    [Fact]
    public async Task PrepareRecruitment_SavedFireteam_TracksItAndKeepsIt()
    {
        var founding = await OpenWithFactionAsync();
        var saved = founding.TeamEntries.Where(team => team.ShowTrackingRadioButton).Last().Name;
        var viewModel = new CohesiveCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.Sectorials);
        viewModel.PrepareRecruitment(new CompanyRecruitment("c.json", "s.json", new SavedCompanyFile
        {
            CompanyName = "Blue Team",
            CompanyType = "Cohesive Company",
            CoreFireteam = saved,
            SourceFactions = [new SavedCompanyFaction { FactionId = SelectionTestKit.Sectorial, FactionName = "Sectorial" }]
        }, 50, []));
        await SelectionTestKit.OpenAsync(viewModel);

        viewModel.TeamEntries.First(team => team.ShowTrackingRadioButton && team.Name != saved).IsTrackedTeam = true;

        Assert.Equal(saved, viewModel.TrackedFireteamName);
    }

    [Fact]
    public async Task SelectedStartSeasonPoints_Changed_RechecksTheSectorials()
    {
        var viewModel = await OpenWithFactionAsync();
        _kit.SpecOps.ClearReceivedCalls();

        viewModel.SelectedStartSeasonPoints = "150";
        await Task.Yield();

        _kit.SpecOps.Received().GetCCFactionFireteamValidity(Arg.Is<string>(key => key.Contains("pts:150")), Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }
}
