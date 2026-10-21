using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Import;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Splash;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels.Splash;

/// <summary>
/// Every launch checks the army data before the app opens: updates are stored when accepted (or at once when nothing
/// is stored), and a failed download offers Retry, plus Continue when stored data can be used instead.
/// </summary>
public sealed class SplashPageViewModelTests
{
    private const string HomeRoute = "//MercsSeasonPage";

    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IArmyDataUpdater _updater = Substitute.For<IArmyDataUpdater>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    [Fact]
    public async Task InitializeAsync_UpToDate_OpensTheAppWithoutAsking()
    {
        Stored(true);
        Checks(ArmyDataChecks.UpToDate());

        await LaunchAsync();

        await _navigation.Received(1).NavigateToAsync(HomeRoute);
        await NoDialogsShownAsync();
        await _updater.DidNotReceive().ApplyAsync(Arg.Any<ArmyDataCheck>(), Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        _updater.Received(1).RebuildOutdatedGeneratedCompanies(Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_FirstDownload_StoresWithoutAsking()
    {
        Stored(false);
        var check = ArmyDataChecks.WithUpdate();
        Checks(check);
        Applies(check);

        await LaunchAsync();

        await _updater.Received(1).ApplyAsync(check, Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        await NoDialogsShownAsync();
        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    [Fact]
    public async Task InitializeAsync_UpdateAccepted_StoresIt()
    {
        Stored(true);
        var check = ArmyDataChecks.WithUpdate();
        Checks(check);
        Applies(check);
        _dialogs.ConfirmAsync("Army Data Update", Arg.Is<string>(message => message.Contains("PanOceania: 7.1 → 7.2")), "Update", "Not Now")
            .Returns(true);

        await LaunchAsync();

        await _updater.Received(1).ApplyAsync(check, Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    [Fact]
    public async Task InitializeAsync_UpdateDeclined_StoresNothing()
    {
        Stored(true);
        Checks(ArmyDataChecks.WithUpdate());
        _dialogs.ConfirmAsync("Army Data Update", Arg.Any<string>(), "Update", "Not Now").Returns(false);

        await LaunchAsync();

        await _updater.DidNotReceive().ApplyAsync(Arg.Any<ArmyDataCheck>(), Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    [Fact]
    public async Task FailedDownload_WithNothingStored_OnlyOffersRetry()
    {
        Stored(false);
        Checks(ArmyDataChecks.Failed(), ArmyDataChecks.UpToDate());

        await LaunchAsync();

        await _dialogs.Received(1).ShowAlertAsync("No Army Data", Arg.Is<string>(message => message.StartsWith("offline")), "Retry");
        await _dialogs.DidNotReceive().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _updater.Received(2).CheckAsync(Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    [Fact]
    public async Task FailedDownload_WithDataStored_CanContinueWithIt()
    {
        Stored(true);
        Checks(ArmyDataChecks.Failed());
        _dialogs.ConfirmAsync("Using Stored Army Data", Arg.Any<string>(), "Continue", "Retry").Returns(true);

        await LaunchAsync();

        await _updater.Received(1).CheckAsync(Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    [Fact]
    public async Task FailedDownload_WithDataStored_CanRetry()
    {
        Stored(true);
        Checks(ArmyDataChecks.Failed(), ArmyDataChecks.UpToDate());
        _dialogs.ConfirmAsync("Using Stored Army Data", Arg.Any<string>(), "Continue", "Retry").Returns(false);

        await LaunchAsync();

        await _updater.Received(2).CheckAsync(Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    [Fact]
    public async Task UnavailableArmies_AreNamed_AndCanBeRetried()
    {
        Stored(true);
        Checks(ArmyDataChecks.WithUnavailable("Tohaa"), ArmyDataChecks.UpToDate());
        _dialogs.ConfirmAsync("Some Armies Unavailable", Arg.Is<string>(message => message.Contains("Tohaa")), "Continue", "Retry")
            .Returns(false);

        await LaunchAsync();

        await _updater.Received(2).CheckAsync(Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    [Fact]
    public async Task StoringThatFails_WithDataStored_CanContinue()
    {
        Stored(true);
        var check = ArmyDataChecks.WithUpdate();
        Checks(check);
        _dialogs.ConfirmAsync("Army Data Update", Arg.Any<string>(), "Update", "Not Now").Returns(true);
        _updater.ApplyAsync(check, Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("disk full"));
        _dialogs.ConfirmAsync("Using Stored Army Data", Arg.Is<string>(message => message.Contains("disk full")), "Continue", "Retry").Returns(true);

        await LaunchAsync();

        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    [Fact]
    public async Task InitializeAsync_ArmiesNotStored_NamesThem()
    {
        Stored(true);
        var check = ArmyDataChecks.WithUpdate();
        Checks(check);
        _dialogs.ConfirmAsync("Army Data Update", Arg.Any<string>(), "Update", "Not Now").Returns(true);
        _updater.ApplyAsync(check, Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>()).Returns(["PanOceania"]);
        _dialogs.ConfirmAsync("Using Stored Army Data", Arg.Is<string>(message => message.Contains("couldn't be stored: PanOceania")), "Continue", "Retry")
            .Returns(true);

        await LaunchAsync();

        await _navigation.Received(1).NavigateToAsync(HomeRoute);
    }

    private async Task LaunchAsync()
    {
        var viewModel = new SplashPageViewModel(_updater, _dialogs, _navigation);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
    }

    private void Stored(bool hasStoredData) => _updater.HasStoredData().Returns(hasStoredData);

    private void Checks(ArmyDataCheck first, params IEnumerable<ArmyDataCheck> then) =>
        _updater.CheckAsync(Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>()).Returns(first, [.. then]);

    private void Applies(ArmyDataCheck check) =>
        _updater.ApplyAsync(check, Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>()).Returns([]);

    private async Task NoDialogsShownAsync()
    {
        await _dialogs.DidNotReceive().ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _dialogs.DidNotReceive().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
