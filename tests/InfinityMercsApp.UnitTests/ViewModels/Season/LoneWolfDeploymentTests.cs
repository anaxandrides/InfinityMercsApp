using System.Text.Json;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Season.Inducements;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Season;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season;

public sealed class LoneWolfDeploymentTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();

    public void Dispose() => _records.Dispose();

    // A Proxy Pack: the Lone Wolf (entry 0) and two Aspects (entries 1 and 2).
    private async Task<PlayModeViewModel> OpenAsync()
    {
        var entries = new List<object>
        {
            new { entryIndex = 0, name = "Kestrel", isLieutenant = true, isLoneWolf = true, sourceFactionId = 101 },
            new { entryIndex = 1, name = "Kestrel (Aspect 2)", aspectOf = 0, sourceFactionId = 101 },
            new { entryIndex = 2, name = "Kestrel (Aspect 3)", aspectOf = 0, sourceFactionId = 101 }
        };
        var companyPath = _records.WriteCompany("c.json", JsonSerializer.Serialize(new { companyType = "Proxy Pack", entries }));
        var armyData = Army101Data.Create();
        var viewModel = new PlayModeViewModel(
            _navigation, Substitute.For<IDialogService>(),
            new DeploymentRoster(armyData, new CompanyFileStore(), Substitute.For<ISeasonFileService>(), new StoreCatalog(Substitute.For<IStoreProvider>())),
            new HiredTroops(armyData), Substitute.For<IAppSettingsProvider>());
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(companyPath, "/s.json"));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsyncCommand_ProxyPack_StartsTheLoneWolfInItsOwnLoadout()
    {
        var viewModel = await OpenAsync();

        Assert.True(viewModel.IsProxyPack);
        Assert.Same(viewModel.DeploymentUnits[0], viewModel.StartingLoneWolf);
        Assert.Equal([0], viewModel.LoneWolfLoadouts.Select(loadout => loadout.EntryIndex));
        Assert.True(viewModel.DeploymentUnits[0].IsCheckboxEnabled);
    }

    [Fact]
    public async Task IsChecked_OwnLoadoutLeftBehind_AnotherLoadoutCarriesTheLoneWolf()
    {
        var viewModel = await OpenAsync();
        viewModel.DeploymentUnits[2].IsChecked = true;

        viewModel.DeploymentUnits[0].IsChecked = false;

        Assert.False(viewModel.DeploymentUnits[0].IsChecked);
        Assert.Equal([2], viewModel.LoneWolfLoadouts.Select(loadout => loadout.EntryIndex));
        Assert.Same(viewModel.DeploymentUnits[2], viewModel.StartingLoneWolf);
    }

    [Fact]
    public async Task IsChecked_LastLoadoutUnchecked_StaysDeployed()
    {
        var viewModel = await OpenAsync();

        viewModel.DeploymentUnits[0].IsChecked = false;

        Assert.True(viewModel.DeploymentUnits[0].IsChecked);
    }

    [Fact]
    public async Task DeployCommand_PickedLoadout_StartsTheLoneWolfThere()
    {
        var viewModel = await OpenAsync();
        viewModel.DeploymentUnits[1].IsChecked = true;
        viewModel.StartingLoneWolf = viewModel.LoneWolfLoadouts.Single(loadout => loadout.EntryIndex == 1);

        await viewModel.DeployCommand.ExecuteAsync(null);

        await _navigation.Received(1).PushAsync("GameModePage", Arg.Is<IDictionary<string, object>>(p =>
            (int)p[NavigationParameters.LoneWolfStart] == 1));
    }
}
