using System.Text.Json;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Presentation.Leagues;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Leagues;
using NSubstitute;
using static InfinityMercsApp.UnitTests.ViewModels.Leagues.LeagueReplies;

namespace InfinityMercsApp.UnitTests.ViewModels.Leagues;

public sealed class LeagueViewModelTests : IDisposable
{
    /// <summary>The renown of the Red Company written below: its one trooper's cost.</summary>
    private const int RedRenown = 28;

    private readonly TempRecords _records = new();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ILeaguesApi _leagues = Substitute.For<ILeaguesApi>();
    private readonly ISignInService _signIn = Substitute.For<ISignInService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ISyncCoordinator _sync = Substitute.For<ISyncCoordinator>();

    public LeagueViewModelTests()
    {
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        WriteSeason("red", "season-red", "Red Company", 28);
    }

    public void Dispose() => _records.Dispose();

    private void WriteSeason(string name, string seasonId, string companyName, int cost)
    {
        var company = _records.WriteCompany(
            $"{name}.json",
            $$"""{"companyName":"{{companyName}}","companyIdentifier":"c-{{name}}","entries":[{"entryIndex":0,"name":"Bulleteer","cost":{{cost}},"isLieutenant":true}]}""");
        _records.WriteSeason(
            $"{name}.json",
            $$"""{"seasonIdentifier":"{{seasonId}}","companyName":"{{companyName}}","companyIdentifier":"c-{{name}}","companyFilePath":{{JsonSerializer.Serialize(company)}}}""");
    }

    private async Task<LeagueViewModel> OpenAsync(LeagueView league)
    {
        _leagues.GetAsync("l1", null, "token", Arg.Any<CancellationToken>()).Returns(Ok(league));
        var companies = new CompanyFileStore(Substitute.For<ISyncOutbox>());
        var roster = new DeploymentRoster(Army101Data.Create(), companies, Substitute.For<ISeasonFileService>(), new StoreCatalog(Substitute.For<IStoreProvider>()));
        var viewModel = new LeagueViewModel(
            _navigation,
            _leagues,
            _signIn,
            _dialogs,
            new SeasonRecordCatalog(_records.Paths),
            new LeagueRenown(companies, Substitute.For<IArmyDataLoader>(), roster),
            _sync);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForLeague("l1"));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_League_ShowsItsStandingsGamesAndMembers()
    {
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        Assert.Equal("Spring League", viewModel.Name);
        Assert.Equal("Season 1", viewModel.CurrentSeasonText);
        Assert.Equal(["Blue Company", "Red Company"], viewModel.Standings.Select(line => line.CompanyName));
        Assert.Equal("1-0-0", viewModel.Standings[1].Record);
        Assert.Equal("Red Company 7 (won) · Blue Company 3", Assert.Single(viewModel.Games).Score);
        Assert.Equal(["Olive", "Mo"], viewModel.Members.Select(member => member.Name));
    }

    [Fact]
    public async Task InitializeAsync_RenownChanged_ReportsTheCompanysRenownNow()
    {
        _leagues.EnterAsync("l1", "season-red", Arg.Any<LeagueEntryUpdate>(), "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown)));

        var viewModel = await OpenAsync(Sample(ownRenown: 300));

        await _leagues.Received(1).EnterAsync("l1", "season-red", Arg.Is<LeagueEntryUpdate>(update => update.Renown == RedRenown), "token", Arg.Any<CancellationToken>());
        Assert.Equal(RedRenown, viewModel.Standings.Single(line => line.IsYours).Renown);
    }

    [Fact]
    public async Task InitializeAsync_AnyLeague_SyncsTheUsersSeasonsFirst()
    {
        await OpenAsync(Sample(ownRenown: RedRenown));

        Received.InOrder(() =>
        {
            _sync.SyncNowAsync();
            _leagues.GetAsync("l1", null, "token", Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task InitializeAsync_RenownUnchanged_ReportsNothing()
    {
        await OpenAsync(Sample(ownRenown: RedRenown));

        await _leagues.DidNotReceive().EnterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LeagueEntryUpdate>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_Organizer_ShowsTheInviteWithItsLinkAndQrCode()
    {
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        Assert.True(viewModel.HasInvite);
        Assert.Equal("ABC234", viewModel.InviteCode);
        Assert.Equal("https://www.infinitythemercs.com/join/ABC234", viewModel.InviteLink);
        Assert.NotNull(viewModel.InviteQr);
    }

    [Fact]
    public async Task InitializeAsync_Member_ShowsNoInvite()
    {
        var viewModel = await OpenAsync(Sample(organizer: false, ownRenown: RedRenown));

        Assert.False(viewModel.HasInvite);
        Assert.Null(viewModel.InviteQr);
        Assert.False(viewModel.StartNextSeasonCommand.CanExecute(null));
    }

    [Fact]
    public async Task RefreshCommand_NoLongerInTheLeague_SaysSo()
    {
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));
        _leagues.GetAsync("l1", null, "token", Arg.Any<CancellationToken>()).Returns(Failed<LeagueView>(LeaguesStatus.NotFound));

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Contains("isn't there", viewModel.Status);
    }

    [Fact]
    public async Task SeasonsToEnter_OwnSeasonNotEntered_IsOffered()
    {
        WriteSeason("green", "season-green", "Green Company", 20);

        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        Assert.Equal("season-green", Assert.Single(viewModel.SeasonsToEnter).Season.SeasonIdentifier);
        Assert.Equal("season-green", viewModel.SeasonToEnter?.Season.SeasonIdentifier);
    }

    [Fact]
    public async Task EnterSeasonCommand_Chosen_SyncsThenEntersItWithItsRenown()
    {
        WriteSeason("green", "season-green", "Green Company", 20);
        _leagues.EnterAsync("l1", "season-green", Arg.Any<LeagueEntryUpdate>(), "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown)));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));
        _sync.ClearReceivedCalls();

        await viewModel.EnterSeasonCommand.ExecuteAsync(null);

        Received.InOrder(() =>
        {
            _sync.SyncNowAsync();
            _leagues.EnterAsync("l1", "season-green", Arg.Is<LeagueEntryUpdate>(update => update.Renown == 20), "token", Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task RecordGameCommand_Win_RecordsALossForTheOpponent()
    {
        _leagues.RecordGameAsync("l1", Arg.Any<NewLeagueGame>(), "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown)));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));
        viewModel.Opponent = Assert.Single(viewModel.Opponents);
        viewModel.Result = ResultChoice.All[0];
        viewModel.OwnObjectivePoints = 8;
        viewModel.OpponentObjectivePoints = 2;
        viewModel.Mission = " Frontline ";
        viewModel.PlayedOn = new DateTime(2026, 11, 7);

        await viewModel.RecordGameCommand.ExecuteAsync(null);

        await _leagues.Received(1).RecordGameAsync(
            "l1",
            Arg.Is<NewLeagueGame>(game =>
                game.Mission == "Frontline" &&
                game.PlayedOn == new DateOnly(2026, 11, 7) &&
                game.Sides[0].SeasonId == "season-red" && game.Sides[0].Result == GameResult.Win && game.Sides[0].ObjectivePoints == 8 &&
                game.Sides[1].SeasonId == "season-blue" && game.Sides[1].Result == GameResult.Loss && game.Sides[1].ObjectivePoints == 2),
            "token",
            Arg.Any<CancellationToken>());
        Assert.Equal("Game recorded.", viewModel.Status);
        Assert.Empty(viewModel.Mission);
    }

    [Fact]
    public async Task RecordGameCommand_NoOpponentChosen_CannotRun()
    {
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        Assert.True(viewModel.CanRecordGames);
        Assert.False(viewModel.RecordGameCommand.CanExecute(null));
    }

    [Fact]
    public async Task RecordGameCommand_Rejected_ShowsWhy()
    {
        _leagues.RecordGameAsync("l1", Arg.Any<NewLeagueGame>(), "token", Arg.Any<CancellationToken>())
            .Returns(Failed<LeagueView>(LeaguesStatus.Rejected, "Blue Company was in an earlier league season."));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));
        viewModel.Opponent = viewModel.Opponents[0];

        await viewModel.RecordGameCommand.ExecuteAsync(null);

        Assert.Equal("Blue Company was in an earlier league season.", viewModel.Status);
    }

    [Fact]
    public async Task RemoveGameCommand_Confirmed_RemovesIt()
    {
        _leagues.RemoveGameAsync("l1", "g1", "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown)));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        await viewModel.RemoveGameCommand.ExecuteAsync(viewModel.Games[0]);

        await _leagues.Received(1).RemoveGameAsync("l1", "g1", "token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveGameCommand_NotConfirmed_KeepsIt()
    {
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        await viewModel.RemoveGameCommand.ExecuteAsync(viewModel.Games[0]);

        await _leagues.DidNotReceive().RemoveGameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawCommand_Confirmed_TakesTheCompanyOut()
    {
        _leagues.WithdrawAsync("l1", "season-red", "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown)));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        await viewModel.WithdrawCommand.ExecuteAsync(viewModel.Standings.Single(line => line.IsYours));

        await _leagues.Received(1).WithdrawAsync("l1", "season-red", "token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ViewCompanyCommand_Standing_OpensTheCompany()
    {
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        await viewModel.ViewCompanyCommand.ExecuteAsync(viewModel.Standings[0]);

        await _navigation.Received(1).PushAsync(
            LeagueViewModel.LeagueCompanyPage,
            Arg.Is<IDictionary<string, object>>(p => (string)p[NavigationParameters.SeasonId] == "season-blue" && (string)p[NavigationParameters.LeagueId] == "l1"));
    }

    [Fact]
    public async Task StartNextSeasonCommand_Confirmed_StartsItAndShowsIt()
    {
        var next = Sample(ownRenown: RedRenown, currentSeason: 2);
        _leagues.StartSeasonAsync("l1", 1, "token", Arg.Any<CancellationToken>()).Returns(Ok(next));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        await viewModel.StartNextSeasonCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.ViewedSeason?.Number);
        Assert.Empty(viewModel.Standings);
        Assert.Equal([2, 1], viewModel.LeagueSeasons.Select(season => season.Number));
        Assert.Empty(viewModel.OwnEntries);
        Assert.False(viewModel.CanRecordGames);
    }

    [Fact]
    public async Task ViewedSeason_EarlierSeason_FetchesAndShowsItsStandings()
    {
        _leagues.GetAsync("l1", 1, "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown, currentSeason: 2, season: 1)));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown, currentSeason: 2));

        viewModel.ViewedSeason = viewModel.LeagueSeasons.Single(season => season.Number == 1);

        Assert.Equal(2, viewModel.Standings.Count);
        Assert.Single(viewModel.Games);
        Assert.False(viewModel.IsViewingCurrentSeason);
        Assert.False(viewModel.HasSeasonsToEnter);
        Assert.False(viewModel.CanRecordGames);
    }

    [Fact]
    public async Task ViewedSeason_EarlierSeasonUnavailable_KeepsTheShownSeasonChosen()
    {
        _leagues.GetAsync("l1", 1, "token", Arg.Any<CancellationToken>()).Returns(Failed<LeagueView>(LeaguesStatus.Unavailable));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown, currentSeason: 2));

        viewModel.ViewedSeason = viewModel.LeagueSeasons.Single(season => season.Number == 1);

        Assert.Equal(2, viewModel.ViewedSeason?.Number);
        Assert.NotEmpty(viewModel.Status);
    }

    [Fact]
    public async Task ViewedSeason_SeasonAlreadyShown_FetchesNothing()
    {
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        viewModel.ViewedSeason = viewModel.LeagueSeasons.Single(season => season.Number == 1);

        await _leagues.Received(1).GetAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshCommand_EarlierSeasonShown_FetchesItAgain()
    {
        _leagues.GetAsync("l1", 1, "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown, currentSeason: 2, season: 1)));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown, currentSeason: 2));
        viewModel.ViewedSeason = viewModel.LeagueSeasons.Single(season => season.Number == 1);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        await _leagues.Received(2).GetAsync("l1", 1, "token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeasonsToEnter_SeasonEnteredInAnEarlierLeagueSeason_IsNotOffered()
    {
        WriteSeason("green", "season-green", "Green Company", 20);

        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown, currentSeason: 2));

        Assert.Equal(["season-green"], viewModel.SeasonsToEnter.Select(choice => choice.Season.SeasonIdentifier));
    }

    [Fact]
    public async Task NewInviteCommand_Confirmed_ShowsTheNewCode()
    {
        _leagues.NewInviteAsync("l1", "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown, inviteCode: "NEW234")));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        await viewModel.NewInviteCommand.ExecuteAsync(null);

        Assert.Equal("NEW234", viewModel.InviteCode);
        Assert.EndsWith("/join/NEW234", viewModel.InviteLink);
    }

    [Fact]
    public async Task CloseInviteCommand_Organizer_HidesTheInvite()
    {
        _leagues.CloseInviteAsync("l1", "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(ownRenown: RedRenown, inviteCode: null)));
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));

        await viewModel.CloseInviteCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasInvite);
        Assert.Null(viewModel.InviteQr);
        Assert.False(viewModel.CloseInviteCommand.CanExecute(null));
    }

    [Fact]
    public async Task RefreshCommand_SignedOut_AsksToLogIn()
    {
        var viewModel = await OpenAsync(Sample(ownRenown: RedRenown));
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns((string?)null);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(LeagueMessages.SignInFirst, viewModel.Status);
    }
}
