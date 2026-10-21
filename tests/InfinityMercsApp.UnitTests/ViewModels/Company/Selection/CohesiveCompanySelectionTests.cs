using InfinityMercsApp.Domain.Fireteams;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class CohesiveCompanySelectionTests
{
    private readonly SelectionTestKit _kit = new();

    private Task<CompanySelectionViewModel> OpenAsync() =>
        SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new CohesiveCompanyRules(ArmySourceSelectionMode.Sectorials)));

    private async Task<CompanySelectionViewModel> OpenWithFactionAsync()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);
        return viewModel;
    }

    private static CoreFireteamTracker Tracker(CompanySelectionViewModel viewModel) =>
        Assert.IsType<CoreFireteamTracker>(viewModel.CoreFireteam);

    // Whether the unit fits the tracked fireteam (or a wildcard), as the company checks it.
    private static bool CanJoinTracked(CompanySelectionViewModel viewModel, ArmyUnitSelectionItem unit)
    {
        var slots = viewModel.UnitList.TeamEntries
            .Where(team => team.IsTrackedTeam || team.IsWildcardBucket)
            .SelectMany(team => team.AllowedProfiles)
            .Select(slot => new FireteamSlot(slot.Name, slot.ResolvedUnitId, slot.ResolvedSourceFactionId, slot.Max))
            .ToList();
        return FireteamLevels.CanJoin(new FireteamMember(unit.Name, unit.Name, unit.Id, unit.SourceFactionId), slots);
    }

    private static async Task<ViewerProfileItem> SelectLieutenantAsync(CompanySelectionViewModel viewModel, bool canJoin)
    {
        viewModel.UnitList.TeamsView = false;
        viewModel.ApplyUnitFilterCommand.Execute(new UnitFilterCriteria { LieutenantOnlyUnits = true });
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(
            viewModel.UnitList.Units.First(unit => unit.IsVisible && CanJoinTracked(viewModel, unit) == canJoin));
        return viewModel.Preview.Profiles.First(profile => profile.IsLieutenant && profile.IsVisible);
    }

    [Fact]
    public async Task InitializeAsync_Sectorials_OffersThoseWithACoreFireteamAndCaches()
    {
        var viewModel = await OpenAsync();

        Assert.Equal("Cohesive Company", viewModel.Rules.CompanyTypeLabel);
        Assert.Equal("One Core fireteam per company", viewModel.FireteamRuleText);
        Assert.True(viewModel.TracksCoreFireteam);
        Assert.False(viewModel.Slots.HasRightSlot);
        Assert.Equal([SelectionTestKit.Sectorial], viewModel.Slots.Factions.Select(faction => faction.Id));
        _kit.SpecOps.Received(1).UpsertCCFactionFireteamValidity(
            SelectionTestKit.Sectorial, Arg.Is<string>(key => key.Contains("pts:75")), true, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectFactionCommand_Faction_OpensOnFireteams()
    {
        var viewModel = await OpenWithFactionAsync();

        Assert.True(viewModel.UnitList.TeamsView);
        Assert.True(viewModel.UnitList.ShowTeamsList);
        Assert.False(viewModel.UnitList.ShowUnitsList);
        Assert.NotEmpty(viewModel.UnitList.TeamEntries);
        Assert.All(viewModel.UnitList.TeamEntries.Where(team => !team.IsWildcardBucket), team => Assert.StartsWith("C: ", team.TeamCountsText));
        Assert.All(viewModel.UnitList.TeamEntries.SelectMany(team => team.AllowedProfiles), slot => Assert.Equal("0", slot.Min));
    }

    [Fact]
    public async Task TrackedFireteamName_UnitsLoaded_TracksOneByDefault()
    {
        var viewModel = await OpenWithFactionAsync();
        var trackable = viewModel.UnitList.TeamEntries.Where(team => team.ShowTrackingRadioButton).ToList();

        Assert.Equal(trackable[0].Name, Tracker(viewModel).Name);
        Assert.True(trackable[0].IsTrackedTeam);

        if (trackable.Count > 1)
        {
            trackable[1].IsTrackedTeam = true;
            Assert.Equal(trackable[1].Name, Tracker(viewModel).Name);
            Assert.False(trackable[0].IsTrackedTeam);
        }
    }

    [Fact]
    public async Task SetTrackedFireteam_Null_ReadsAsSelectFireteam()
    {
        var viewModel = await OpenWithFactionAsync();

        Tracker(viewModel).Track(null);

        Assert.Equal("Select fireteam", Tracker(viewModel).NameDisplay);
        Assert.Equal(0, Tracker(viewModel).Level);
        Assert.Null(Tracker(viewModel).LevelIcon);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_Trooper_JoinsWithNumberTwoAndLieutenantFtMaster()
    {
        var viewModel = await OpenWithFactionAsync();
        var lieutenant = await SelectLieutenantAsync(viewModel, canJoin: true);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(lieutenant);

        var entry = Assert.Single(viewModel.Roster.Entries);
        Assert.Contains(CohesiveCompanyRules.NumberTwoSkill, entry.SavedSkills);
        Assert.Contains(CohesiveCompanyRules.FireteamMasterSkill, entry.SavedSkills);
        Assert.False(entry.IsIrregular);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_FirstTrooper_HidesWhoCantJoinAndTheOtherFireteams()
    {
        var viewModel = await OpenWithFactionAsync();
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await SelectLieutenantAsync(viewModel, canJoin: true));

        Assert.Single(viewModel.Roster.Entries);
        Assert.All(viewModel.UnitList.Units.Where(unit => unit.IsVisible), unit => Assert.True(CanJoinTracked(viewModel, unit)));
        Assert.Contains(viewModel.UnitList.Units, unit => !unit.IsVisible && !CanJoinTracked(viewModel, unit));
        Assert.All(
            viewModel.UnitList.TeamEntries.Where(team => !team.IsWildcardBucket && !team.IsTrackedTeam),
            team => Assert.False(team.IsVisible));
    }

    [Fact]
    public async Task ApplyUnitVisibilityFilters_NoTroopers_ShowsWhoCantJoin()
    {
        var viewModel = await OpenWithFactionAsync();
        viewModel.UnitList.TeamsView = false;

        viewModel.ApplyUnitVisibilityFilters();

        Assert.Contains(viewModel.UnitList.Units, unit => unit.IsVisible && !CanJoinTracked(viewModel, unit));
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_CannotJoinTheFireteam_TurnsItAway()
    {
        var viewModel = await OpenWithFactionAsync();
        var lieutenant = await SelectLieutenantAsync(viewModel, canJoin: false);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(lieutenant);

        Assert.Empty(viewModel.Roster.Entries);
        await _kit.Dialogs.Received(1).ShowAlertAsync("Can't Add Trooper", Arg.Is<string>(text => text.Contains(Tracker(viewModel).Name)));
    }

    [Fact]
    public async Task PrepareRecruitment_SavedFireteam_TracksItAndKeepsIt()
    {
        var founding = await OpenWithFactionAsync();
        var saved = founding.UnitList.TeamEntries.Where(team => team.ShowTrackingRadioButton).Last().Name;
        var viewModel = new CompanySelectionViewModel(_kit.Services, new CohesiveCompanyRules(ArmySourceSelectionMode.Sectorials));
        viewModel.PrepareRecruitment(new CompanyRecruitment("c.json", "s.json", new SavedCompanyFile
        {
            CompanyName = "Blue Team",
            CompanyType = "Cohesive Company",
            CoreFireteam = saved,
            SourceFactions = [new SavedCompanyFaction { FactionId = SelectionTestKit.Sectorial, FactionName = "Sectorial" }]
        }, 50, []));
        await SelectionTestKit.OpenAsync(viewModel);

        viewModel.UnitList.TeamEntries.First(team => team.ShowTrackingRadioButton && team.Name != saved).IsTrackedTeam = true;

        Assert.Equal(saved, Tracker(viewModel).Name);
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
