using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.ViewModels.Leagues;
using NSubstitute;
using static InfinityMercsApp.UnitTests.ViewModels.Leagues.LeagueReplies;

namespace InfinityMercsApp.UnitTests.ViewModels.Leagues;

public sealed class LeagueCompanyViewModelTests
{
    private readonly ILeaguesApi _leagues = Substitute.For<ILeaguesApi>();
    private readonly ISignInService _signIn = Substitute.For<ISignInService>();

    public LeagueCompanyViewModelTests() => _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");

    private async Task<LeagueCompanyViewModel> OpenAsync()
    {
        var viewModel = new LeagueCompanyViewModel(Substitute.For<INavigationService>(), _leagues, _signIn);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForLeague("l1", "season-blue"));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_Entered_ShowsTheCompanyAndItsSeason()
    {
        _leagues.EntryFilesAsync("l1", "season-blue", "token", Arg.Any<CancellationToken>()).Returns(Ok(new LeagueEntryFiles
        {
            Company = new SavedCompanyFile
            {
                CompanyName = "Blue Company",
                CompanyIdentifier = "c1",
                Entries = [new SavedCompanyEntry { EntryIndex = 0, Name = "Bulleteer", IsLieutenant = true }]
            },
            Season = new SeasonFile { CompanyName = "Blue Company", Rounds = [new SeasonRound { RoundIndex = 1, MissionResults = new SeasonMissionResult { Won = true } }] }
        }));

        var viewModel = await OpenAsync();

        Assert.Equal("Blue Company", viewModel.Title);
        Assert.Equal("Bulleteer", Assert.Single(viewModel.Company?.Troopers ?? []).Name);
        Assert.Equal(1, viewModel.Season?.Wins);
        Assert.Empty(viewModel.Status);
    }

    [Fact]
    public async Task InitializeAsync_OwnerDeletedIt_SaysSo()
    {
        _leagues.EntryFilesAsync("l1", "season-blue", "token", Arg.Any<CancellationToken>()).Returns(Ok(new LeagueEntryFiles()));

        var viewModel = await OpenAsync();

        Assert.Contains("deleted", viewModel.Status);
    }

    [Fact]
    public async Task InitializeAsync_NotInTheLeague_SaysSo()
    {
        _leagues.EntryFilesAsync("l1", "season-blue", "token", Arg.Any<CancellationToken>()).Returns(Failed<LeagueEntryFiles>(LeaguesStatus.NotFound));

        var viewModel = await OpenAsync();

        Assert.Contains("isn't there", viewModel.Status);
    }

    [Fact]
    public async Task InitializeAsync_SignedOut_AsksToLogIn()
    {
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns((string?)null);

        var viewModel = await OpenAsync();

        Assert.Contains("Log in", viewModel.Status);
    }
}
