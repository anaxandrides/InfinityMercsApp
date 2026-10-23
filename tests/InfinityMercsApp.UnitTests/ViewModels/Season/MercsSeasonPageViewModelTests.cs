using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.ViewModels.Season;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season;

public sealed class MercsSeasonPageViewModelTests
{
    private readonly ISignInService _signIn = Substitute.For<ISignInService>();
    private readonly ISyncCoordinator _sync = Substitute.For<ISyncCoordinator>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    public MercsSeasonPageViewModelTests() => _signIn.IsAvailable.Returns(true);

    private async Task<MercsSeasonPageViewModel> OpenAsync()
    {
        var viewModel = new MercsSeasonPageViewModel(Substitute.For<INavigationService>(), _signIn, _sync, _dialogs);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_LoggedOut_OffersLogIn()
    {
        var viewModel = await OpenAsync();

        Assert.True(viewModel.ShowLogIn);
        Assert.False(viewModel.ShowAccount);
        Assert.True(viewModel.LogInCommand.CanExecute(null));
    }

    [Fact]
    public async Task InitializeAsync_LoggedIn_SaysWhoAs()
    {
        _signIn.GetAccountAsync(Arg.Any<CancellationToken>()).Returns(new SignedInAccount("kestrel@example.com", "Kestrel"));

        var viewModel = await OpenAsync();

        Assert.False(viewModel.ShowLogIn);
        Assert.True(viewModel.ShowAccount);
        Assert.Equal("Logged in as Kestrel", viewModel.AccountText);
    }

    [Fact]
    public async Task InitializeAsync_DeviceCannotSignIn_HidesLogIn()
    {
        _signIn.IsAvailable.Returns(false);

        var viewModel = await OpenAsync();

        Assert.False(viewModel.ShowLogIn);
    }

    [Fact]
    public async Task LogInCommand_SignedIn_ShowsTheNameAndSyncs()
    {
        _signIn.SignInAsync(Arg.Any<CancellationToken>()).Returns(SignInOutcome.SignedIn(new SignedInAccount("kestrel@example.com", "Kestrel")));
        _sync.SyncNowAsync().Returns(SyncReport.SignedOut);
        var viewModel = await OpenAsync();

        await viewModel.LogInCommand.ExecuteAsync(null);

        Assert.Equal("Logged in as Kestrel", viewModel.AccountText);
        Assert.False(viewModel.ShowLogIn);
        await _sync.Received(1).SyncNowAsync();
    }

    [Fact]
    public async Task LogInCommand_Failed_SaysWhyAndStaysLoggedOut()
    {
        _signIn.SignInAsync(Arg.Any<CancellationToken>()).Returns(SignInOutcome.Failed("Signing in failed: no network"));
        var viewModel = await OpenAsync();

        await viewModel.LogInCommand.ExecuteAsync(null);

        Assert.True(viewModel.ShowLogIn);
        await _dialogs.Received(1).ShowAlertAsync("Not Logged In", "Signing in failed: no network");
    }
}
