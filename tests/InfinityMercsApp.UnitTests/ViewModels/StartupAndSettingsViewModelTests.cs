using System.Runtime.CompilerServices;
using InfinityMercsApp.Domain.Models.DataImport;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

internal static class ImportStubs
{
    public static async IAsyncEnumerable<SuccessWithStringResult> Messages(params string[] messages)
    {
        foreach (var message in messages)
        {
            await Task.Yield();
            yield return new SuccessWithStringResult(true, message);
        }
    }

    public static async IAsyncEnumerable<SuccessWithStringResult> Failing(
        string message, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}

public sealed class SettingsViewModelTests
{
    private readonly IAppSettingsProvider _settings = Substitute.For<IAppSettingsProvider>();
    private readonly IImportService _import = Substitute.For<IImportService>();

    private async Task<SettingsViewModel> OpenAsync(bool storedInches)
    {
        _settings.GetShowUnitsInInches().Returns(storedInches);
        var viewModel = new SettingsViewModel(new FakeNavigationService(), _settings, _import);
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

public sealed class SplashPageViewModelTests
{
    private readonly FakeNavigationService _navigation = new();
    private readonly IFactionProvider _factions = Substitute.For<IFactionProvider>();
    private readonly IImportService _import = Substitute.For<IImportService>();
    private readonly IServiceProvider _services = Substitute.For<IServiceProvider>();

    public SplashPageViewModelTests() => _services.GetService(typeof(IImportService)).Returns(_import);

    [Fact]
    public async Task FirstLaunchImportsBeforeOpeningTheHomePage()
    {
        _factions.HasAnyFactionData().Returns(false);
        _import.ImportAllDataAsync().Returns(ImportStubs.Messages("Importing factions"));
        var viewModel = new SplashPageViewModel(_services, _factions, _navigation);

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        Assert.Equal("Importing factions", viewModel.UpdateProgressMessage);
        Assert.Equal("//MercsSeasonPage", Assert.Single(_navigation.Navigations).Route);
    }

    [Fact]
    public async Task LaterLaunchesOpenTheHomePageAndSyncInTheBackground()
    {
        _factions.HasAnyFactionData().Returns(true);
        _import.ImportAllDataAsync().Returns(ImportStubs.Messages());
        var viewModel = new SplashPageViewModel(_services, _factions, _navigation);

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        Assert.Equal("//MercsSeasonPage", Assert.Single(_navigation.Navigations).Route);
        _import.Received(1).ImportAllDataAsync();
    }
}
