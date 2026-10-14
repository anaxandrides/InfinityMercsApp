using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class SettingsViewModelTests
{
    private readonly IAppSettingsProvider _settings = Substitute.For<IAppSettingsProvider>();
    private readonly IImportService _import = Substitute.For<IImportService>();

    private async Task<SettingsViewModel> OpenAsync(bool storedInches)
    {
        _settings.GetShowUnitsInInches().Returns(storedInches);
        var viewModel = new SettingsViewModel(Substitute.For<INavigationService>(), _settings, _import);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task LoadsTheStoredUnitsWithoutSavingThemBack()
    {
        var viewModel = await OpenAsync(storedInches: false);

        Assert.False(viewModel.ShowUnitsInInches);
        Assert.True(viewModel.ShowUnitsInCentimeters);
        _settings.DidNotReceive().SetShowUnitsInInches(Arg.Any<bool>());
    }

    [Fact]
    public async Task ChoosingCentimetersSavesTheSetting()
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
    public async Task ForcedUpdateReportsEachImportMessage()
    {
        _import.ImportAllDataAsync(true).Returns(ImportStubs.Messages("Metadata", "Done"));
        var viewModel = await OpenAsync(storedInches: true);

        await viewModel.UpdateAllDataCommand.ExecuteAsync(null);

        Assert.Equal("Done", viewModel.UpdateStatus);
        Assert.False(viewModel.IsUpdateInProgress);
        Assert.Equal(string.Empty, viewModel.UpdateProgressMessage);
        Assert.True(viewModel.UpdateAllDataCommand.CanExecute(null));
    }

    [Fact]
    public async Task ForcedUpdateFailureIsShown()
    {
        _import.ImportAllDataAsync(true).Returns(ImportStubs.Failing("offline"));
        var viewModel = await OpenAsync(storedInches: true);

        await viewModel.UpdateAllDataCommand.ExecuteAsync(null);

        Assert.Equal("Update failed: offline", viewModel.UpdateStatus);
        Assert.False(viewModel.IsUpdateInProgress);
    }
}
