using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Presentation.Leagues;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.ViewModels.Leagues;
using NSubstitute;
using static InfinityMercsApp.UnitTests.ViewModels.Leagues.LeagueReplies;

namespace InfinityMercsApp.UnitTests.ViewModels.Leagues;

public sealed class LeaguesViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ILeaguesApi _leagues = Substitute.For<ILeaguesApi>();
    private readonly ISignInService _signIn = Substitute.For<ISignInService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    public LeaguesViewModelTests()
    {
        _signIn.IsAvailable.Returns(true);
        _signIn.GetAccountAsync(Arg.Any<CancellationToken>()).Returns(new SignedInAccount("olive@example.com", "Olive"));
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        _leagues.ListAsync("token", Arg.Any<CancellationToken>()).Returns(Ok(new List<LeagueSummary> { new() { LeagueId = "l1", Name = "Spring League" } }));
    }

    private async Task<LeaguesViewModel> OpenAsync()
    {
        var viewModel = new LeaguesViewModel(_navigation, _leagues, _signIn, _dialogs);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_SignedIn_ListsTheirLeagues()
    {
        var viewModel = await OpenAsync();

        Assert.Equal("Spring League", Assert.Single(viewModel.Leagues).Name);
        Assert.False(viewModel.HasNoLeagues);
        Assert.Empty(viewModel.Status);
    }

    [Fact]
    public async Task InitializeAsync_SignedOut_AsksToLogIn()
    {
        _signIn.GetAccountAsync(Arg.Any<CancellationToken>()).Returns((SignedInAccount?)null);

        var viewModel = await OpenAsync();

        Assert.True(viewModel.ShowLogIn);
        Assert.Equal(LeagueMessages.SignInFirst, viewModel.Status);
        await _leagues.DidNotReceive().ListAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_NoLeagues_SaysHowToGetOne()
    {
        _leagues.ListAsync("token", Arg.Any<CancellationToken>()).Returns(Ok(new List<LeagueSummary>()));

        var viewModel = await OpenAsync();

        Assert.True(viewModel.HasNoLeagues);
        Assert.Contains("join one", viewModel.Status);
    }

    [Fact]
    public async Task InitializeAsync_ServerDown_SaysSo()
    {
        _leagues.ListAsync("token", Arg.Any<CancellationToken>()).Returns(Failed<List<LeagueSummary>>(LeaguesStatus.Unavailable));

        var viewModel = await OpenAsync();

        Assert.Contains("couldn't be reached", viewModel.Status);
    }

    [Fact]
    public async Task LogInCommand_SignsIn_ListsTheirLeagues()
    {
        _signIn.GetAccountAsync(Arg.Any<CancellationToken>()).Returns((SignedInAccount?)null);
        _signIn.SignInAsync(Arg.Any<CancellationToken>()).Returns(SignInOutcome.SignedIn(new SignedInAccount("olive@example.com", "Olive")));
        var viewModel = await OpenAsync();

        await viewModel.LogInCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsSignedIn);
        Assert.Single(viewModel.Leagues);
    }

    [Fact]
    public async Task CreateLeagueCommand_NamedLeague_StartsItAsTheUserAndOpensIt()
    {
        _leagues.CreateAsync(Arg.Any<NewLeague>(), "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample()));
        var viewModel = await OpenAsync();
        viewModel.NewLeagueName = " Spring League ";

        await viewModel.CreateLeagueCommand.ExecuteAsync(null);

        await _leagues.Received(1).CreateAsync(Arg.Is<NewLeague>(league => league.Name == "Spring League" && league.DisplayName == "Olive"), "token", Arg.Any<CancellationToken>());
        await _navigation.Received(1).PushAsync(LeaguesViewModel.LeaguePage, Arg.Is<IDictionary<string, object>>(p => (string)p[NavigationParameters.LeagueId] == "l1"));
        Assert.Empty(viewModel.NewLeagueName);
    }

    [Fact]
    public async Task CreateLeagueCommand_Rejected_ShowsWhy()
    {
        _leagues.CreateAsync(Arg.Any<NewLeague>(), "token", Arg.Any<CancellationToken>()).Returns(Failed<LeagueView>(LeaguesStatus.Rejected, "The league's name is too long."));
        var viewModel = await OpenAsync();
        viewModel.NewLeagueName = "Spring";

        await viewModel.CreateLeagueCommand.ExecuteAsync(null);

        Assert.Equal("The league's name is too long.", viewModel.Status);
    }

    [Fact]
    public async Task CreateLeagueCommand_NoName_CannotRun()
    {
        var viewModel = await OpenAsync();
        viewModel.NewLeagueName = "   ";

        Assert.False(viewModel.CreateLeagueCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("abc-234", true)]
    [InlineData("abc", false)]
    public async Task JoinCommand_TypedCode_RunsOnlyForACode(string typed, bool runs)
    {
        var viewModel = await OpenAsync();

        viewModel.InviteCode = typed;

        Assert.Equal(runs, viewModel.JoinCommand.CanExecute(null));
    }

    [Fact]
    public async Task JoinCommand_TypedCode_OpensTheInvite()
    {
        var viewModel = await OpenAsync();
        viewModel.InviteCode = "abc 234";

        await viewModel.JoinCommand.ExecuteAsync(null);

        await _navigation.Received(1).PushAsync(LeaguesViewModel.JoinLeaguePage, Arg.Is<IDictionary<string, object>>(p => (string)p[NavigationParameters.InviteCode] == "ABC234"));
    }

    [Fact]
    public async Task OpenCommand_League_OpensIt()
    {
        var viewModel = await OpenAsync();

        await viewModel.OpenCommand.ExecuteAsync(viewModel.Leagues[0]);

        await _navigation.Received(1).PushAsync(LeaguesViewModel.LeaguePage, Arg.Any<IDictionary<string, object>>());
    }
}
