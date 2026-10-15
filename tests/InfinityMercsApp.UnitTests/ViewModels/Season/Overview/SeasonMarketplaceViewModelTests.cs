using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.ViewModels.Marketplaces;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class SeasonMarketplaceViewModelTests
{
    private static readonly IReadOnlySet<string> PanOceania = new HashSet<string> { "PanOceania" };

    private readonly SeasonTestKit _kit = new();
    private readonly SeasonMarketplaceViewModel _market;
    private int _purchases;

    public SeasonMarketplaceViewModelTests()
    {
        _market = new SeasonMarketplaceViewModel(_kit.Stores, _kit.SeasonFiles, _kit.Dialogs, _kit.Popup(), () =>
        {
            _purchases++;
            return Task.CompletedTask;
        });
    }

    private async Task OpenAsync()
    {
        _market.RefreshBalance(_kit.Season);
        await _market.LoadStoresAsync(PanOceania, SeasonTestKit.SeasonPath, _kit.Season);
    }

    private async Task ShowAsync(string storeName)
    {
        _market.SelectedStore = _market.Stores.Single(store => store.StoreName == storeName);
        await (_market.SelectStoreCommand.ExecutionTask ?? Task.CompletedTask);
    }

    private StoreItemRow Item(string name) => _market.ItemGroups.SelectMany(group => group.Items).Single(item => item.Name == name);

    // Three rounds, each a 10/20/30 CR reward, so 60 CR and 1.5 SWC to spend.
    private void GiveThreeRounds() =>
        _kit.Season.Rounds = [new SeasonRound { RoundIndex = 1 }, new SeasonRound { RoundIndex = 2 }, new SeasonRound { RoundIndex = 3 }];

    [Fact]
    public async Task ListsMarketsThenTheTemporaryStoreThenServices()
    {
        _kit.Season.Rounds[0].TemporaryStore = "Jade Temu";

        await OpenAsync();

        Assert.Equal(
            ["Number One [PanO, Lawful]", "Jade Temu - Round 1 Store", "🩺 Medical Services", "Additional Recruitment"],
            _market.Stores.Select(store => store.Label));
        Assert.Equal(["Additional Recruitment", "Number One", "Medical Services", "Jade Temu"], _market.StoreNames());
        Assert.Same(_market.Stores[0], _market.SelectedStore);
        Assert.Equal("Number One", _market.StoreName);
    }

    [Fact]
    public async Task ATemporaryStoreTheCompanyHasIsNotListedTwice()
    {
        _kit.Season.Rounds[0].TemporaryStore = "number one";

        await OpenAsync();

        Assert.Equal(3, _market.Stores.Count);
    }

    [Fact]
    public async Task ShowsTheStoresStockByCategory()
    {
        await OpenAsync();

        Assert.Equal(["PRIMARY", "SIDEARM", "EXCHANGE"], _market.ItemGroups.Select(group => group.Category));
        Assert.Equal("Light Armour (LI)", _market.TroopTypeRows.Single().DisplayName);
        Assert.Equal(["WIP=12", "Courage"], _market.AugmentRows.Select(augment => augment.Name));
        Assert.Equal("Lawful", _market.AlignmentText);
        Assert.True(_market.HasTroopTypes && _market.HasAugments && _market.HasAlignmentText);
    }

    [Fact]
    public async Task NotorietyMovesPricesAtLawfulAndChaoticMarkets()
    {
        _kit.Season.Rounds[0].Downtime.NotorietyGain = 2;
        _kit.Season.Rounds[0].TemporaryStore = "Jade Temu";
        await OpenAsync();

        Assert.Equal(14, Item("Combi Rifle").CostCr);
        Assert.Equal("Lawful  (Notoriety: +2 CR)", _market.AlignmentText);

        await ShowAsync("Jade Temu");
        Assert.Equal(6, Item("Monofilament Blade").CostCr);
        Assert.Equal("Chaotic  (Notoriety: -2 CR)", _market.AlignmentText);
    }

    [Fact]
    public async Task TheCartComesOutOfTheBalance()
    {
        GiveThreeRounds();
        await OpenAsync();
        Assert.Equal("60 CR - 1.5 SWC", _market.ResourcesText);

        _market.IncrementCommand.Execute(Item("Spitfire"));
        _market.IncrementCommand.Execute(Item("Combi Rifle"));

        Assert.Equal("28 CR - 0.5 SWC", _market.ResourcesText);
        Assert.Equal(1, Item("Spitfire").Quantity);
        Assert.True(_market.HasCartItems);
        Assert.True(_market.BuyCommand.CanExecute(null));
    }

    [Fact]
    public async Task WhatNoLongerFitsCannotBeAdded()
    {
        await OpenAsync();
        var rifle = Item("Combi Rifle");
        Assert.False(rifle.IsAffordable);

        _market.IncrementCommand.Execute(rifle);

        Assert.Equal(0, rifle.Quantity);
        Assert.False(_market.HasCartItems);
        Assert.False(_market.BuyCommand.CanExecute(null));
    }

    [Fact]
    public async Task RemovingFromTheCartGivesTheMoneyBack()
    {
        await OpenAsync();
        var pistol = Item("Pistol");
        _market.IncrementCommand.Execute(pistol);

        _market.DecrementCommand.Execute(pistol);
        _market.DecrementCommand.Execute(pistol);

        Assert.Equal(0, pistol.Quantity);
        Assert.Equal("10 CR - 0.5 SWC", _market.ResourcesText);
        Assert.False(_market.HasCartItems);
    }

    [Fact]
    public async Task TheCartIsKeptAcrossStores()
    {
        GiveThreeRounds();
        _kit.Season.Rounds[2].TemporaryStore = "Jade Temu";
        await OpenAsync();
        _market.IncrementCommand.Execute(Item("Pistol"));

        await ShowAsync("Jade Temu");
        _market.IncrementCommand.Execute(Item("Monofilament Blade"));
        await ShowAsync("Number One");

        Assert.Equal(1, Item("Pistol").Quantity);
        Assert.Equal("49 CR - 1.5 SWC", _market.ResourcesText);
    }

    [Fact]
    public async Task BuyingRecordsTheCartInTheLatestRound()
    {
        GiveThreeRounds();
        await OpenAsync();
        _market.IncrementCommand.Execute(Item("Pistol"));
        _market.IncrementCommand.Execute(Item("Pistol"));
        _market.IncrementCommand.Execute(Item("SWC +1"));
        _market.IncrementCommand.Execute(_market.TroopTypeRows[0]);

        await _market.BuyCommand.ExecuteAsync(null);

        var round = _kit.Season.Rounds[2];
        Assert.Equal(["Pistol", "Pistol", "SWC +1", "Light Armour (LI)"], round.Marketplace.Transactions.Select(transaction => transaction.ItemName));
        Assert.All(round.Marketplace.Transactions, transaction => Assert.Equal("Number One", transaction.OriginStore));
        Assert.Equal(1m, round.Marketplace.Transactions[2].SwcGrant);
        Assert.Null(round.Marketplace.Transactions[0].SwcGrant);
        Assert.Equal((60, 1.5), (round.StartingCr, round.StartingSwc));
        Assert.Contains("Number One", round.Marketplace.Stores);
        Assert.Equal(0, Item("Pistol").Quantity);
        Assert.False(_market.HasCartItems);
        Assert.Equal(1, _purchases);
    }

    [Fact]
    public async Task ItemsCostingSwcRecordIt()
    {
        GiveThreeRounds();
        await OpenAsync();
        _market.IncrementCommand.Execute(Item("Spitfire"));

        await _market.BuyCommand.ExecuteAsync(null);

        Assert.Equal(1m, _kit.Season.Rounds[2].Marketplace.Transactions.Single().CostSwc);
    }

    [Fact]
    public async Task BuyingBeforeAnyRoundSaysSoAndKeepsTheCart()
    {
        GiveThreeRounds();
        await OpenAsync();
        _market.IncrementCommand.Execute(Item("Pistol"));
        _kit.Season.Rounds.Clear();

        await _market.BuyCommand.ExecuteAsync(null);

        await _kit.Dialogs.Received(1).ShowAlertAsync("Nothing Bought", Arg.Any<string>());
        Assert.True(_market.HasCartItems);
        Assert.Equal(0, _purchases);
    }

    [Fact]
    public async Task ALaterPickWinsOverASlowerStore()
    {
        var slow = new TaskCompletionSource<Store?>();
        _kit.Season.Rounds[0].TemporaryStore = "Jade Temu";
        await OpenAsync();
        _kit.Stores.GetStoreByNameAsync("Jade Temu", Arg.Any<CancellationToken>()).Returns(slow.Task);

        _market.SelectedStore = _market.Stores.Single(store => store.StoreName == "Jade Temu");
        await ShowAsync(ServiceStoreNames.MedicalServices);
        slow.SetResult(SeasonTestKit.JadeTemu);

        Assert.Equal(ServiceStoreNames.MedicalServices, _market.StoreName);
    }

    [Fact]
    public async Task TappingARowShowsItsDetail()
    {
        await OpenAsync();

        await _market.ShowDetailCommand.ExecuteAsync(_market.AugmentRows[0]);

        Assert.Equal("WIP=12", _market.DetailPopup.Detail?.Title);
    }

    [Fact]
    public async Task ChangingStoreClosesTheDetail()
    {
        await OpenAsync();
        await _market.ShowDetailCommand.ExecuteAsync(_market.AugmentRows[0]);

        await ShowAsync(ServiceStoreNames.MedicalServices);

        Assert.False(_market.DetailPopup.IsOpen);
    }
}
