using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.ViewModels.Leagues;
using NSubstitute;
using static InfinityMercsApp.UnitTests.ViewModels.Leagues.LeagueReplies;

namespace InfinityMercsApp.UnitTests.ViewModels.Leagues;

public sealed class JoinLeagueViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ILeaguesApi _leagues = Substitute.For<ILeaguesApi>();
    private readonly ISignInService _signIn = Substitute.For<ISignInService>();

    public JoinLeagueViewModelTests()
    {
        _signIn.IsAvailable.Returns(true);
        _signIn.GetAccountAsync(Arg.Any<CancellationToken>()).Returns(new SignedInAccount("nova@example.com", "nova@example.com"));
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        _leagues.PreviewInviteAsync("ABC234", "token", Arg.Any<CancellationToken>())
            .Returns(Ok(new LeagueInvitePreview { LeagueId = "l1", Name = "Spring League", OrganizerName = "Olive", MemberCount = 1 }));
    }

    private async Task<JoinLeagueViewModel> OpenAsync(string code = "abc-234")
    {
        var viewModel = new JoinLeagueViewModel(_navigation, _leagues, _signIn, Substitute.For<IDialogService>());
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParameters.InviteCode] = code });
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_GoodCode_ShowsTheLeague()
    {
        var viewModel = await OpenAsync();

        Assert.Equal("Spring League", viewModel.Preview?.Name);
        Assert.Equal("Run by Olive · 1 member", viewModel.Details);
        Assert.True(viewModel.JoinCommand.CanExecute(null));
    }

    [Fact]
    public async Task InitializeAsync_UnknownCode_SaysTheInviteIsGone()
    {
        _leagues.PreviewInviteAsync("XYZ789", "token", Arg.Any<CancellationToken>()).Returns(Failed<LeagueInvitePreview>(LeaguesStatus.NotFound));

        var viewModel = await OpenAsync("XYZ789");

        Assert.False(viewModel.HasLeague);
        Assert.Equal(JoinLeagueViewModel.InvalidInvite, viewModel.Status);
    }

    [Fact]
    public async Task InitializeAsync_NotACode_SaysTheInviteIsGone()
    {
        var viewModel = await OpenAsync("nope");

        Assert.Equal(JoinLeagueViewModel.InvalidInvite, viewModel.Status);
        await _leagues.DidNotReceive().PreviewInviteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_SignedOut_AsksToLogInFirst()
    {
        _signIn.GetAccountAsync(Arg.Any<CancellationToken>()).Returns((SignedInAccount?)null);

        var viewModel = await OpenAsync();

        Assert.True(viewModel.ShowLogIn);
        Assert.False(viewModel.HasLeague);
    }

    [Fact]
    public async Task LogInCommand_SignsIn_ShowsTheLeague()
    {
        _signIn.GetAccountAsync(Arg.Any<CancellationToken>()).Returns((SignedInAccount?)null);
        _signIn.SignInAsync(Arg.Any<CancellationToken>()).Returns(SignInOutcome.SignedIn(new SignedInAccount("nova@example.com", "Nova")));
        var viewModel = await OpenAsync();

        await viewModel.LogInCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasLeague);
    }

    [Fact]
    public async Task JoinCommand_GoodCode_JoinsByTheAccountsNameAndOpensTheLeague()
    {
        _leagues.JoinAsync("ABC234", Arg.Any<JoinLeague>(), "token", Arg.Any<CancellationToken>()).Returns(Ok(Sample(organizer: false)));
        var viewModel = await OpenAsync();

        await viewModel.JoinCommand.ExecuteAsync(null);

        await _leagues.Received(1).JoinAsync("ABC234", Arg.Is<JoinLeague>(join => join.DisplayName == "nova"), "token", Arg.Any<CancellationToken>());
        await _navigation.Received(1).PushAsync(LeaguesViewModel.LeaguePage, Arg.Is<IDictionary<string, object>>(p => (string)p[NavigationParameters.LeagueId] == "l1"));
    }

    [Fact]
    public async Task JoinCommand_AlreadyAMember_OpensTheLeagueWithoutJoining()
    {
        _leagues.PreviewInviteAsync("ABC234", "token", Arg.Any<CancellationToken>())
            .Returns(Ok(new LeagueInvitePreview { LeagueId = "l1", Name = "Spring League", IsMember = true }));
        var viewModel = await OpenAsync();

        await viewModel.JoinCommand.ExecuteAsync(null);

        await _leagues.DidNotReceive().JoinAsync(Arg.Any<string>(), Arg.Any<JoinLeague>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _navigation.Received(1).PushAsync(LeaguesViewModel.LeaguePage, Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task JoinCommand_CodeReplacedMeanwhile_SaysTheInviteIsGone()
    {
        _leagues.JoinAsync("ABC234", Arg.Any<JoinLeague>(), "token", Arg.Any<CancellationToken>()).Returns(Failed<LeagueView>(LeaguesStatus.NotFound));
        var viewModel = await OpenAsync();

        await viewModel.JoinCommand.ExecuteAsync(null);

        Assert.Equal(JoinLeagueViewModel.InvalidInvite, viewModel.Status);
    }

    [Fact]
    public async Task JoinCommand_LeagueFull_ShowsWhy()
    {
        _leagues.JoinAsync("ABC234", Arg.Any<JoinLeague>(), "token", Arg.Any<CancellationToken>()).Returns(Failed<LeagueView>(LeaguesStatus.Rejected, "The league is full."));
        var viewModel = await OpenAsync();

        await viewModel.JoinCommand.ExecuteAsync(null);

        Assert.Equal("The league is full.", viewModel.Status);
    }
}
