using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Import;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Settings;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Settings;

public sealed class SettingsViewModelTests
{
    private readonly IAppSettingsProvider _settings = Substitute.For<IAppSettingsProvider>();
    private readonly IArmyDataUpdater _updater = Substitute.For<IArmyDataUpdater>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private async Task<SettingsViewModel> OpenAsync(bool storedInches)
    {
        _settings.GetShowUnitsInInches().Returns(storedInches);
        var viewModel = new SettingsViewModel(Substitute.For<INavigationService>(), _settings, _updater, _dialogs);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsync_StoredUnits_LoadsWithoutSavingBack()
    {
        var viewModel = await OpenAsync(storedInches: false);

        Assert.False(viewModel.ShowUnitsInInches);
        Assert.True(viewModel.ShowUnitsInCentimeters);
        _settings.DidNotReceive().SetShowUnitsInInches(Arg.Any<bool>());
    }

    [Fact]
    public async Task ShowUnitsInCentimeters_Set_SavesTheSetting()
    {
        var viewModel = await OpenAsync(storedInches: true);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.ShowUnitsInCentimeters = true;

        Assert.False(viewModel.ShowUnitsInInches);
        _settings.Received(1).SetShowUnitsInInches(false);
        Assert.Contains(nameof(SettingsViewModel.ShowUnitsInCentimeters), changed);
    }

    [Fact]
    public async Task UpdateAllDataCommand_UpToDate_ReportsWithoutAsking()
    {
        Checks(ArmyDataChecks.UpToDate());
        var viewModel = await OpenAsync(storedInches: true);

        await viewModel.UpdateAllDataCommand.ExecuteAsync(null);

        Assert.Equal("The army data is up to date.", viewModel.UpdateStatus);
        await _dialogs.DidNotReceive().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        Assert.False(viewModel.IsUpdateInProgress);
        Assert.Equal(string.Empty, viewModel.UpdateProgressMessage);
        Assert.True(viewModel.UpdateAllDataCommand.CanExecute(null));
    }

    [Fact]
    public async Task UpdateAllDataCommand_UpdateAccepted_StoresIt()
    {
        var check = ArmyDataChecks.WithUpdate();
        Checks(check);
        _dialogs.ConfirmAsync("Army Data Update", Arg.Is<string>(message => message.Contains("PanOceania: 7.1 → 7.2")), "Update", "Not Now").Returns(true);
        _updater.ApplyAsync(check, Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>()).Returns([]);
        var viewModel = await OpenAsync(storedInches: true);

        await viewModel.UpdateAllDataCommand.ExecuteAsync(null);

        await _updater.Received(1).ApplyAsync(check, Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        Assert.Equal("The army data is updated.", viewModel.UpdateStatus);
    }

    [Fact]
    public async Task UpdateAllDataCommand_UpdateDeclined_KeepsTheStoredData()
    {
        Checks(ArmyDataChecks.WithUpdate());
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var viewModel = await OpenAsync(storedInches: true);

        await viewModel.UpdateAllDataCommand.ExecuteAsync(null);

        await _updater.DidNotReceive().ApplyAsync(Arg.Any<ArmyDataCheck>(), Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>());
        Assert.Equal("The stored army data was kept.", viewModel.UpdateStatus);
    }

    [Fact]
    public async Task UpdateAllDataCommand_ArmiesUnavailable_NamesThem()
    {
        Checks(ArmyDataChecks.WithUnavailable("Ariadna", "Tohaa"));
        var viewModel = await OpenAsync(storedInches: true);

        await viewModel.UpdateAllDataCommand.ExecuteAsync(null);

        Assert.Equal("The army data is up to date. These armies couldn't be downloaded: Ariadna, Tohaa.", viewModel.UpdateStatus);
    }

    [Fact]
    public async Task UpdateAllDataCommand_DownloadFails_ShowsIt()
    {
        Checks(ArmyDataChecks.Failed());
        var viewModel = await OpenAsync(storedInches: true);

        await viewModel.UpdateAllDataCommand.ExecuteAsync(null);

        Assert.Equal("offline", viewModel.UpdateStatus);
        Assert.False(viewModel.IsUpdateInProgress);
    }

    [Fact]
    public async Task UpdateAllDataCommand_UpdateThrows_ShowsIt()
    {
        _updater.CheckAsync(Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>()).Returns<ArmyDataCheck>(_ => throw new InvalidOperationException("disk full"));
        var viewModel = await OpenAsync(storedInches: true);

        await viewModel.UpdateAllDataCommand.ExecuteAsync(null);

        Assert.Equal("Update failed: disk full", viewModel.UpdateStatus);
        Assert.False(viewModel.IsUpdateInProgress);
    }

    private void Checks(ArmyDataCheck check) =>
        _updater.CheckAsync(Arg.Any<IProgress<ArmyDataProgress>>(), Arg.Any<CancellationToken>()).Returns(check);
}
