using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class SeasonViewModelTests
{
    private readonly SeasonTestKit _kit = new();

    [Fact]
    public async Task OpeningASeasonShowsTheCompanyAndItsRound()
    {
        _kit.Season.Rounds[0].TemporaryStore = "Jade Temu";

        var viewModel = await _kit.OpenAsync();

        Assert.Equal("Blue Team", viewModel.CompanyName);
        Assert.Equal("Standard Company", viewModel.CompanyType);
        Assert.Equal("Round 1 Marketplace", viewModel.RoundTitle);
        Assert.Equal(["Captain", "Bolt", "Traktor Mul"], viewModel.Team.Units.Select(unit => unit.Name));
        Assert.Equal("Captain", viewModel.Team.SelectedName);
        Assert.True(viewModel.Marketplace.HasStore);
        Assert.False(viewModel.StoreSearch.IsOpen);
        await _kit.SeasonFiles.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!);
    }

    [Fact]
    public async Task ACompanyWithoutASeasonStartsOne()
    {
        _kit.Season = new SeasonFile();

        var viewModel = await _kit.OpenAsync(withSeasonFile: false);

        await _kit.SeasonFiles.Received(1).CreateAsync("Blue Team", "blue-1", SeasonTestKit.CompanyPath);
        Assert.Equal("Round 0", viewModel.RoundTitle);
        Assert.False(viewModel.StoreSearch.IsOpen);
    }

    [Fact]
    public async Task AMissingCompanyShowsAnEmptySeason()
    {
        _kit.CompanyFiles.Exists(SeasonTestKit.CompanyPath).Returns(false);

        var viewModel = await _kit.OpenAsync();

        Assert.Equal(SeasonViewModel.DefaultName, viewModel.CompanyName);
        Assert.Empty(viewModel.Team.Units);
    }

    [Fact]
    public async Task AnUnreadableCompanyShowsAnEmptySeason()
    {
        _kit.CompanyFiles.LoadAsync(SeasonTestKit.CompanyPath).ThrowsAsync(new IOException("corrupt"));

        var viewModel = await _kit.OpenAsync();

        Assert.Equal(SeasonViewModel.DefaultName, viewModel.CompanyName);
        Assert.Empty(viewModel.Team.Units);
    }

    [Fact]
    public async Task TheCompanyLoadsOnceAndTheSeasonIsReReadOnEachAppearance()
    {
        var viewModel = await _kit.OpenAsync();
        _kit.Season.Rounds.Add(new SeasonRound { RoundIndex = 2 });

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        await _kit.CompanyFiles.Received(1).LoadAsync(SeasonTestKit.CompanyPath);
        Assert.Equal("Round 2 Marketplace", viewModel.RoundTitle);
    }

    [Fact]
    public async Task ARoundWithoutATemporaryStoreAsksForOne()
    {
        var viewModel = await _kit.OpenAsync();

        Assert.True(viewModel.StoreSearch.IsOpen);
        Assert.DoesNotContain("Number One", viewModel.StoreSearch.Choices);
        Assert.Contains("Jade Temu", viewModel.StoreSearch.Choices);
    }

    [Fact]
    public async Task ChoosingATemporaryStoreAddsItToTheMarkets()
    {
        var viewModel = await _kit.OpenAsync();
        _kit.Dice.RollD20().Returns(3);
        viewModel.StoreSearch.RollCommand.Execute(null);

        await viewModel.StoreSearch.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("Jade Temu", _kit.Season.Rounds[0].TemporaryStore);
        Assert.False(viewModel.StoreSearch.IsOpen);
        Assert.Contains(viewModel.Marketplace.Stores, store => store.Label == "Jade Temu - Round 1 Store");
    }

    [Fact]
    public async Task TheBalanceShowsWhatIsLeftToSpend()
    {
        var viewModel = await _kit.OpenAsync();

        Assert.Equal("10 CR - 0.5 SWC", viewModel.Marketplace.ResourcesText);
    }

    [Fact]
    public async Task OwnedGearReachesTheInventoryAndTheGearPickers()
    {
        _kit.Season.Rounds[0].Marketplace.Transactions.Add(SeasonTestKit.Bought("Number One", "Pistol"));

        var viewModel = await _kit.OpenAsync();

        var section = Assert.Single(viewModel.Inventory.Sections);
        Assert.Equal("SIDEARM", section.Title);
        var sidearm = viewModel.Team.GearSlots.Single(picker => picker.Title == "SIDEARM");
        Assert.Equal(["(none)", "Pistol"], sidearm.Options.Select(option => option.Display));
    }

    [Fact]
    public async Task SellingRefreshesTheInventory()
    {
        _kit.Season.Rounds[0].Marketplace.Transactions.Add(SeasonTestKit.Bought("Number One", "Pistol"));
        _kit.Dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var viewModel = await _kit.OpenAsync();

        await viewModel.Inventory.SellCommand.ExecuteAsync(viewModel.Inventory.Sections[0].Items[0]);

        Assert.Empty(viewModel.Inventory.Sections);
        Assert.Equal(SeasonInventoryViewModel.EmptyMessage, viewModel.Inventory.Message);
    }

    [Fact]
    public async Task BuyingRefreshesTheInventory()
    {
        var viewModel = await _kit.OpenAsync();
        var pistol = viewModel.Marketplace.ItemGroups.Single(group => group.Category == "SIDEARM").Items[0];
        viewModel.Marketplace.IncrementCommand.Execute(pistol);

        await viewModel.Marketplace.BuyCommand.ExecuteAsync(null);

        Assert.Equal("Pistol", Assert.Single(viewModel.Inventory.Sections).Items[0].Name);
    }

    [Fact]
    public async Task TabsSwitch()
    {
        var viewModel = await _kit.OpenAsync();
        Assert.True(viewModel.IsTeamTab);

        await viewModel.ShowTabCommand.ExecuteAsync(SeasonTab.Marketplace);

        Assert.Equal((false, false, true), (viewModel.IsTeamTab, viewModel.IsInventoryTab, viewModel.IsMarketplaceTab));
    }

    [Fact]
    public async Task PlayingARoundOpensPlayModeWithTheCompanyAndSeason()
    {
        var viewModel = await _kit.OpenAsync();

        await viewModel.PlayRoundCommand.ExecuteAsync(null);

        await _kit.Navigation.Received(1).PushAsync("PlayModePage", Arg.Is<IDictionary<string, object>>(parameters =>
            (string)parameters[NavigationParameters.CompanyFilePath] == SeasonTestKit.CompanyPath &&
            (string)parameters[NavigationParameters.SeasonFilePath] == SeasonTestKit.SeasonPath));
    }

    [Fact]
    public async Task NavigatingWithoutACompanyChangesNothing()
    {
        var viewModel = _kit.SeasonViewModel();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>());

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        await _kit.CompanyFiles.DidNotReceiveWithAnyArgs().LoadAsync(default!);
        Assert.Equal(SeasonViewModel.DefaultName, viewModel.CompanyName);
        Assert.Equal("Round 0", viewModel.RoundTitle);
    }

    [Fact]
    public async Task SpendingPerkPointsOpensRankUp()
    {
        var viewModel = await _kit.OpenAsync();

        await viewModel.RankUpCommand.ExecuteAsync(null);

        await _kit.Navigation.Received(1).PushAsync("RankUpPage", Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task MedicalServicesShowsTheServices()
    {
        var viewModel = await _kit.OpenAsync();
        Assert.True(viewModel.Marketplace.ShowsStock);

        viewModel.Marketplace.SelectedStore = viewModel.Marketplace.Stores.Single(store => store.StoreName == "Medical Services");

        Assert.True(viewModel.Marketplace.IsServicesStore);
        Assert.False(viewModel.Marketplace.ShowsStock);
        Assert.Equal(["Captain", "Bolt", "Traktor Mul"], viewModel.Services.Troopers.Select(trooper => trooper.Name));
    }

    [Fact]
    public async Task ATherapyRefreshesTheTeam()
    {
        _kit.Season.TrooperFor(1).Injuries.Add(Injury.Arms);
        _kit.Dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), "Buy", "Cancel").Returns(true);
        var viewModel = await _kit.OpenAsync();
        viewModel.Services.SelectedTrooper = viewModel.Services.Troopers.Single(trooper => trooper.Name == "Bolt");

        await viewModel.Services.BuyCommand.ExecuteAsync(viewModel.Services.Options[0]);

        viewModel.Team.SelectUnitCommand.Execute(viewModel.Team.Units.Single(unit => unit.Name == "Bolt"));
        Assert.False(viewModel.Team.HasInjuries);
    }
}
