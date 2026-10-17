using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.ViewModels.Season.Overview;
using InfinityMercsApp.ViewModels.Season.RankUp;
using NSubstitute;

using InfinityMercsApp.Presentation.Company.Viewer;
namespace InfinityMercsApp.UnitTests.ViewModels.Season.RankUp;

public sealed class RankUpViewModelTests
{
    private readonly SeasonTestKit _kit = new();

    private void GiveXp(int entryIndex, string name, int xp) =>
        _kit.Season.Rounds[0].MissionResults.UnitResults.Add(new SeasonMissionUnitResult { EntryIndex = entryIndex, UnitName = name, XpGained = xp });

    private async Task<RankUpViewModel> OpenAsync()
    {
        var viewModel = new RankUpViewModel(_kit.Navigation, _kit.SeasonFiles, _kit.CompanyFiles, new CompanyRoster(_kit.ArmyData), _kit.Dice);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(SeasonTestKit.CompanyPath, SeasonTestKit.SeasonPath));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task ListsOnlyTroopersWithPointsAndShowsTheFirst()
    {
        GiveXp(1, "Bolt", 16);

        var viewModel = await OpenAsync();

        Assert.Equal(["Bolt"], viewModel.Troopers.Select(trooper => trooper.Name));
        Assert.Equal("Bolt", viewModel.Trooper?.Name);
        Assert.Equal("Hired Gun · 16 XP", viewModel.RankText);
        Assert.Equal("2 perk points to spend", viewModel.PointsText);
        Assert.True(viewModel.IsChoosing && viewModel.CanRoll);
        Assert.DoesNotContain(viewModel.Lists, list => list.ListId == PerkChoices.TagListId);
    }

    [Fact]
    public async Task NobodyRankedUpMeansEverythingIsSpent()
    {
        var viewModel = await OpenAsync();

        Assert.True(viewModel.AllSpent);
        await viewModel.BackToBaseCommand.ExecuteAsync(null);
        await _kit.Navigation.Received(1).PopToRootAsync();
    }

    [Fact]
    public async Task ChoosingOffersTheListsPerksUpToTheRank()
    {
        GiveXp(1, "Bolt", 5);
        var viewModel = await OpenAsync();

        viewModel.SelectedList = viewModel.Lists.Single(list => list.Name == "Body");

        Assert.NotEmpty(viewModel.Options);
        Assert.All(viewModel.Options, option => Assert.Equal(1, option.Pick.Tier));
    }

    [Fact]
    public async Task TakingAPerkSavesItAndMovesOn()
    {
        GiveXp(1, "Bolt", 5);
        GiveXp(3, "Traktor Mul", 5);
        var viewModel = await OpenAsync();
        viewModel.SelectedList = viewModel.Lists.Single(list => list.Name == "Body");
        var climbing = viewModel.Options.Single(option => option.Pick.NodeId == "body-track-1-tier-1");

        await viewModel.TakePerkCommand.ExecuteAsync(climbing);

        Assert.Equal(["body-track-1-tier-1"], _kit.Season.Troopers[1].Perks);
        Assert.Equal(["Traktor Mul"], viewModel.Troopers.Select(trooper => trooper.Name));
        Assert.Equal("Traktor Mul", viewModel.Trooper?.Name);
    }

    [Fact]
    public async Task RollingPicksTheListThenTheTrack()
    {
        GiveXp(1, "Bolt", 5);
        _kit.Dice.RollD20().Returns(8, 4);
        var viewModel = await OpenAsync();

        viewModel.RollListCommand.Execute(null);
        Assert.Equal("List roll 8: Cool or Body", viewModel.RollText);
        viewModel.RollTrackCommand.Execute(viewModel.RolledLists.Single(list => list.Name == "Body"));

        Assert.True(viewModel.IsRolling);
        Assert.Equal("Body: track roll 4", viewModel.RollText);
        Assert.Equal(["body-track-1-tier-1", "body-track-2-tier-1"], viewModel.Options.Select(option => option.Pick.NodeId));
    }

    [Fact]
    public async Task OverlappingListsAreOfferedToPickFrom()
    {
        GiveXp(1, "Bolt", 5);
        _kit.Dice.RollD20().Returns(5, 1);
        var viewModel = await OpenAsync();

        viewModel.RollListCommand.Execute(null);

        Assert.Equal(["Initiative", "Cool"], viewModel.RolledLists.Select(list => list.Name));
        viewModel.RollTrackCommand.Execute(viewModel.RolledLists[1]);
        Assert.False(viewModel.HasRolledLists);
        Assert.StartsWith("Cool: track roll", viewModel.RollText);
    }

    [Fact]
    public async Task ChoosingAgainLeavesTheRoll()
    {
        GiveXp(1, "Bolt", 5);
        _kit.Dice.RollD20().Returns(8, 4);
        var viewModel = await OpenAsync();
        viewModel.RollListCommand.Execute(null);

        viewModel.ChooseCommand.Execute(null);

        Assert.True(viewModel.IsChoosing);
        Assert.Equal(string.Empty, viewModel.RollText);
    }
}
