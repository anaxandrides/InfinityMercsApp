using System.Text.Json;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Season.Inducements;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Season;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season;

public sealed class PlayModeViewModelTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly IAppSettingsProvider _settings = Substitute.For<IAppSettingsProvider>();
    private readonly IArmyDataService _armyData = Army101Data.Create();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private string _companyPath = string.Empty;

    public PlayModeViewModelTests() => _settings.GetShowUnitsInInches().Returns(true);

    public void Dispose() => _records.Dispose();

    /// <summary>A lieutenant (entry 0), then <paramref name="troopers"/> one-slot units and an optional TAG Company TAG.</summary>
    private async Task<PlayModeViewModel> OpenAsync(int troopers, bool withTag = false)
    {
        var entries = new List<object> { new { entryIndex = 0, name = "Lt", isLieutenant = true, sourceFactionId = 101, cost = 20 } };
        entries.AddRange(Enumerable.Range(1, troopers).Select(i => (object)new { entryIndex = i, name = $"T{i}", sourceFactionId = 101, cost = 10 }));
        if (withTag)
        {
            entries.Add(new { entryIndex = 99, name = "Tag", unitTypeCode = "TAG", sourceFactionId = 2003 });
        }

        _companyPath = _records.WriteCompany("c.json", JsonSerializer.Serialize(new { entries }));
        var viewModel = new PlayModeViewModel(
            _navigation, _dialogs, new DeploymentRoster(_armyData, new CompanyFileStore(Substitute.For<ISyncOutbox>()), _seasonFiles, new StoreCatalog(Substitute.For<IStoreProvider>())),
            new HiredTroops(_armyData), _settings);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(_companyPath, "/s.json"));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task InitializeAsyncCommand_Company_DeploysAndSelectsTheLieutenant()
    {
        var viewModel = await OpenAsync(troopers: 2);

        Assert.Equal(1, viewModel.DeployedSlotCount);
        Assert.True(viewModel.IsEliteDeployment);
        Assert.Equal("1 / 6 slots · ELITE", viewModel.DeploymentStatusText);
        Assert.Same(viewModel.DeploymentUnits[0], viewModel.SelectedUnit);
        Assert.True(viewModel.DeploymentUnits[0].IsSelected);
    }

    [Fact]
    public async Task IsChecked_LieutenantUnchecked_StaysChecked()
    {
        var viewModel = await OpenAsync(troopers: 1);

        viewModel.DeploymentUnits[0].IsChecked = false;

        Assert.True(viewModel.DeploymentUnits[0].IsChecked);
    }

    [Fact]
    public async Task IsChecked_EveryTrooper_FillsNoMoreThanSixSlots()
    {
        var viewModel = await OpenAsync(troopers: 4, withTag: true);

        foreach (var unit in viewModel.DeploymentUnits)
        {
            unit.IsChecked = true;
        }

        Assert.Equal(5, viewModel.DeployedSlotCount);
        Assert.False(viewModel.DeploymentUnits.Single(u => u.IsTagFromTagCompany).IsChecked);
        Assert.Equal("5 / 6 slots", viewModel.DeploymentStatusText);
        Assert.False(viewModel.IsEliteDeployment);
    }

    [Fact]
    public async Task SelectUnitCommand_Unit_MovesTheHighlight()
    {
        var viewModel = await OpenAsync(troopers: 1);

        viewModel.SelectUnitCommand.Execute(viewModel.DeploymentUnits[1]);

        Assert.False(viewModel.DeploymentUnits[0].IsSelected);
        Assert.True(viewModel.DeploymentUnits[1].IsSelected);
        Assert.True(viewModel.HasSelectedUnit);
    }

    [Fact]
    public async Task DeployCommand_CheckedUnits_OpensTheGameWithThem()
    {
        var viewModel = await OpenAsync(troopers: 2);
        viewModel.DeploymentUnits[2].IsChecked = true;

        await viewModel.DeployCommand.ExecuteAsync(null);

        await _navigation.Received(1).PushAsync("GameModePage", Arg.Is<IDictionary<string, object>>(p =>
            (string)p[NavigationParameters.CompanyFilePath] == _companyPath &&
            (string)p[NavigationParameters.SeasonFilePath] == "/s.json" &&
            ((IEnumerable<int>)p[NavigationParameters.DeployedIndices]).SequenceEqual(new List<int> { 0, 2 }) &&
            (bool)p[NavigationParameters.EliteDeployment]));
    }

    [Fact]
    public async Task InitializeAsyncCommand_DistanceUnitsChanged_Reloads()
    {
        var viewModel = await OpenAsync(troopers: 1);
        viewModel.DeploymentUnits[1].IsChecked = true;

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        Assert.True(viewModel.DeploymentUnits[1].IsChecked);

        _settings.GetShowUnitsInInches().Returns(false);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        Assert.False(viewModel.DeploymentUnits[1].IsChecked);
    }

    // Hires a Bounty Hunter in its Boarding Shotgun loadout.
    private async Task HireAsync(PlayModeViewModel viewModel)
    {
        _dialogs.ChooseAsync("Hire Authorized Bounty Hunter (SMG or BSG)", "Cancel", Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.ArgAt<IReadOnlyList<string>>(2)[0]);
        await viewModel.Shop.HireCommand.ExecuteAsync(viewModel.Shop.HireOffers[0]);
    }

    // The company's renown is 30 (lieutenant 20, trooper 10); 75 against it is 45 more, half of which rounds down to 20.
    [Fact]
    public async Task OpponentRenownText_HigherRenown_GivesTheCompanyInducements()
    {
        var viewModel = await OpenAsync(troopers: 1);

        viewModel.OpponentRenownText = "75";

        Assert.Equal((30, 20), (viewModel.CompanyRenown, viewModel.Inducements.Received));
        Assert.Equal("You get 20 inducements.", viewModel.InducementText);
        Assert.True(viewModel.CanSpendInducements);
    }

    // Renown stays with the company: a retired trooper no longer deploys, but what it kept still counts (user's ruling).
    [Fact]
    public async Task OpponentRenownText_TrooperRetired_CountsTheRenownItKept()
    {
        _seasonFiles.LoadAsync("/s.json").Returns(new SeasonFile
        {
            CompanyName = "Company",
            CompanyIdentifier = "c",
            Retired = [new RetiredTrooper { EntryIndex = 1, Name = "T1", Renown = 10 }]
        });
        var viewModel = await OpenAsync(troopers: 1);

        viewModel.OpponentRenownText = "75";

        Assert.DoesNotContain(viewModel.DeploymentUnits, unit => unit.EntryIndex == 1);
        Assert.Equal((30, 20), (viewModel.CompanyRenown, viewModel.Inducements.Received));
    }

    // Inducements go by the whole company's renown, not by who is ticked to deploy.
    [Fact]
    public async Task IsChecked_TrooperLeftHome_KeepsTheInducements()
    {
        var viewModel = await OpenAsync(troopers: 1);
        viewModel.OpponentRenownText = "75";

        viewModel.DeploymentUnits[1].IsChecked = true;
        viewModel.DeploymentUnits[1].IsChecked = false;

        Assert.Equal((30, 20), (viewModel.Inducements.CompanyRenown, viewModel.Inducements.Received));
    }

    [Fact]
    public async Task OpponentRenownText_LowerRenown_SaysTheOpponentGetsThem()
    {
        var viewModel = await OpenAsync(troopers: 1);

        viewModel.OpponentRenownText = "0";

        Assert.Equal("Your opponent gets 15 inducements.", viewModel.InducementText);
        Assert.False(viewModel.CanSpendInducements);
    }

    [Fact]
    public async Task IsCooperative_True_GivesNoInducements()
    {
        var viewModel = await OpenAsync(troopers: 1);
        viewModel.OpponentRenownText = "75";

        viewModel.IsCooperative = true;

        Assert.Equal(0, viewModel.Inducements.Received);
        Assert.Equal("A cooperative contract gives no inducements.", viewModel.InducementText);
    }

    [Fact]
    public async Task HireCommand_SixDeployed_TakesTheSeventhPlace()
    {
        var viewModel = await OpenAsync(troopers: 6);
        viewModel.OpponentRenownText = "500";
        foreach (var unit in viewModel.DeploymentUnits.Skip(1).Take(5))
        {
            unit.IsChecked = true;
        }

        await HireAsync(viewModel);

        Assert.Equal("6 / 6 slots + 1 hired", viewModel.DeploymentStatusText);
        Assert.All(viewModel.Shop.HireOffers, offer => Assert.False(offer.IsEnabled));
    }

    [Fact]
    public async Task IsChecked_TwoHired_LeavesRoomForFiveOfTheCompany()
    {
        var viewModel = await OpenAsync(troopers: 6);
        viewModel.OpponentRenownText = "500";
        await HireAsync(viewModel);
        await HireAsync(viewModel);

        foreach (var unit in viewModel.DeploymentUnits)
        {
            unit.IsChecked = true;
        }

        Assert.Equal(5, viewModel.DeployedSlotCount);
    }

    [Fact]
    public async Task IsEliteDeployment_FourDeployedAndHires_StaysElite()
    {
        var viewModel = await OpenAsync(troopers: 3);
        viewModel.OpponentRenownText = "500";
        foreach (var unit in viewModel.DeploymentUnits)
        {
            unit.IsChecked = true;
        }

        await HireAsync(viewModel);
        await HireAsync(viewModel);

        Assert.True(viewModel.IsEliteDeployment);
        Assert.Equal("4 / 6 slots + 2 hired · ELITE", viewModel.DeploymentStatusText);
    }

    [Fact]
    public async Task IsChecked_TrooperWithRentalLeftHome_ReturnsTheRental()
    {
        var viewModel = await OpenAsync(troopers: 1);
        viewModel.OpponentRenownText = "75";
        viewModel.DeploymentUnits[1].IsChecked = true;
        _dialogs.ChooseAsync("Rent Panzerfaust", "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns("T1");
        await viewModel.Shop.RentCommand.ExecuteAsync(viewModel.Shop.RentalOffers.First(offer => offer.Name == "Panzerfaust"));
        Assert.Single(viewModel.Inducements.Rentals);

        viewModel.DeploymentUnits[1].IsChecked = false;

        Assert.Empty(viewModel.Inducements.Rentals);
    }

    [Fact]
    public async Task DeployCommand_Inducements_PassesThemToTheGame()
    {
        var viewModel = await OpenAsync(troopers: 1);
        viewModel.OpponentRenownText = "75";
        await HireAsync(viewModel);

        await viewModel.DeployCommand.ExecuteAsync(null);

        await _navigation.Received(1).PushAsync("GameModePage", Arg.Is<IDictionary<string, object>>(p =>
            ((ContractInducements)p[NavigationParameters.Inducements]).OpponentRenown == 75 &&
            ((ContractInducements)p[NavigationParameters.Inducements]).Hired.Count == 1));
    }

    [Fact]
    public async Task DeployCommand_MoreSpentThanNowReceived_StaysToReturnSomething()
    {
        var viewModel = await OpenAsync(troopers: 1);
        viewModel.OpponentRenownText = "75";
        await HireAsync(viewModel);
        viewModel.OpponentRenownText = "30";

        await viewModel.DeployCommand.ExecuteAsync(null);

        await _dialogs.Received(1).ShowAlertAsync("Too Many Inducements Spent", Arg.Any<string>());
        await _navigation.DidNotReceiveWithAnyArgs().PushAsync(default!, default!);
    }
}
