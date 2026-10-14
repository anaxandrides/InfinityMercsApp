using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class SplashPageViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
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
        await _navigation.Received(1).NavigateToAsync("//MercsSeasonPage");
    }

    [Fact]
    public async Task LaterLaunchesOpenTheHomePageAndSyncInTheBackground()
    {
        _factions.HasAnyFactionData().Returns(true);
        _import.ImportAllDataAsync().Returns(ImportStubs.Messages());
        var viewModel = new SplashPageViewModel(_services, _factions, _navigation);

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        await _navigation.Received(1).NavigateToAsync("//MercsSeasonPage");
        _import.Received(1).ImportAllDataAsync();
    }
}
