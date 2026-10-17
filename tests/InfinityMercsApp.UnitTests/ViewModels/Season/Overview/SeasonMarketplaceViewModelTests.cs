using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;
using InfinityMercsApp.ViewModels.Marketplaces.Items;

using InfinityMercsApp.Services.Rolls;
namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class SeasonMarketplaceViewModelTests
{
    private static readonly IReadOnlySet<string> PanOceania = new HashSet<string> { "PanOceania" };

    private readonly SeasonTestKit _kit = new();
    private readonly SeasonMarketplaceViewModel _market;
    private int _purchases;

    public SeasonMarketplaceViewModelTests()
    {
        _market = new SeasonMarketplaceViewModel(_kit.Stores, _kit.SeasonFiles, _kit.Dialogs, _kit.Popup(), _kit.Rolls(), () =>
        {
            _purchases++;
            return Task.CompletedTask;
        });
    }

    // Opens the market tab with Number One, the company's own market, as this round's War Market, and shows it.
    private async Task OpenAsync(string? warMarket = "Number One")
    {
        if (warMarket is not null)
        {
            SeasonRounds.CurrentMarketplace(_kit.Season).WarMarket = warMarket;
        }

        _market.RefreshBalance(_kit.Season);
        await _market.LoadStoresAsync(PanOceania, SeasonTestKit.SeasonPath, _kit.Season);
        if (warMarket is not null)
        {
            await ShowAsync(_market.Stores[1].StoreName);
        }
    }

    private async Task ShowAsync(string storeName)
    {
        _market.SelectedStore = _market.Stores.Single(store => store.StoreName == storeName);
        await (_market.SelectStoreCommand.ExecutionTask ?? Task.CompletedTask);
    }

    private StoreItemRow Item(string name) => _market.ItemGroups.SelectMany(group => group.Items).Single(item => item.Name == name);

    // Three rounds, each a 10/20/30 CR reward, so 60 CR and 1.5 SWC; the War Market is chosen on the last.
    private void GiveThreeRounds() =>
        _kit.Season.Rounds = [new SeasonRound { RoundIndex = 1 }, new SeasonRound { RoundIndex = 2 }, new SeasonRound { RoundIndex = 3 }];

    [Fact]
    public async Task ListsTheBaseMarketThenTheWarMarketThenServices()
    {
        _kit.Season.Rounds[0].Marketplace.WarMarket = "Number One";

        await _market.LoadStoresAsync(PanOceania, SeasonTestKit.SeasonPath, _kit.Season);

        Assert.Equal(
            ["Base Market [Neutral]", "Number One [PanO, Lawful]", "🩺 Medical Services", "Additional Recruitment"],
            _market.Stores.Select(store => store.Label));
        Assert.Equal(["Base Market", "Number One", "Medical Services", "Additional Recruitment"], _market.StoreNames());
        Assert.Same(_market.Stores[0], _market.SelectedStore);
        Assert.Equal("Base Market", _market.StoreName);
    }

    [Fact]
    public async Task UntilAWarMarketIsChosenOnlyTheBaseMarketAndServicesAreOpen()
    {
        await OpenAsync(warMarket: null);

        Assert.Equal(["Base Market", "Medical Services", "Additional Recruitment"], _market.StoreNames());
    }

    [Fact]
    public async Task TheCompanysFactionMarketsAreKnownForChoosingOne()
    {
        await OpenAsync(warMarket: null);

        Assert.Equal(["Number One"], _market.FactionMarketNames);
    }

    [Fact]
    public async Task ARolledWarMarketFromAnotherFactionIsOpen()
    {
        await OpenAsync("Jade Temu");

        Assert.Equal(["Base Market", "Jade Temu", "Medical Services", "Additional Recruitment"], _market.StoreNames());
    }

    [Fact]
    public async Task AnOlderSeasonsTemporaryStoreIsTheRoundsWarMarket()
    {
        _kit.Season.Rounds[0].TemporaryStore = "Jade Temu";

        await OpenAsync(warMarket: null);

        Assert.Contains("Jade Temu", _market.StoreNames());
        Assert.DoesNotContain("Number One", _market.StoreNames());
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
    public async Task NotorietyRaisesPricesAtALawfulMarket()
    {
        _kit.Season.Rounds[0].Downtime.NotorietyGain = 2;
        await OpenAsync();

        Assert.Equal(14, Item("Combi Rifle").CostCr);
        Assert.Equal("Lawful  (Notoriety: +2 CR)", _market.AlignmentText);
    }

    [Fact]
    public async Task NotorietyLowersPricesAtAChaoticMarket()
    {
        _kit.Season.Rounds[0].Downtime.NotorietyGain = 2;
        await OpenAsync("Jade Temu");

        Assert.Equal(6, Item("Monofilament Blade").CostCr);
        Assert.Equal("Chaotic  (Notoriety: -2 CR)", _market.AlignmentText);
    }

    [Fact]
    public async Task TheCartComesOutOfTheCreditsButNotTheSwc()
    {
        GiveThreeRounds();
        await OpenAsync();
        Assert.Equal("60 CR - 1.5 SWC", _market.ResourcesText);

        _market.IncrementCommand.Execute(Item("Spitfire"));
        _market.IncrementCommand.Execute(Item("Combi Rifle"));

        Assert.Equal("28 CR - 1.5 SWC", _market.ResourcesText);
        Assert.Equal(1, Item("Spitfire").Quantity);
        Assert.True(_market.HasCartItems);
        Assert.True(_market.BuyCommand.CanExecute(null));
        Assert.Equal("BUY · 2 items · 32 CR", _market.BuyText);
    }

    [Fact]
    public async Task GearCostingSwcCanBeBoughtWithoutSwc()
    {
        _kit.Season = new SeasonFile { StartingFunds = new SeasonStartingFunds { Credits = 25 } };
        await OpenAsync();

        Assert.Equal("25 CR - 0 SWC", _market.ResourcesText);
        Assert.True(Item("Spitfire").IsAffordable);
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
        Assert.Equal("BUY", _market.BuyText);
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
        await OpenAsync();
        _market.IncrementCommand.Execute(Item("Pistol"));

        await ShowAsync("Base Market");
        _market.IncrementCommand.Execute(Item("Knife"));
        await ShowAsync("Number One");

        Assert.Equal(1, Item("Pistol").Quantity);
        Assert.Equal("55 CR - 1.5 SWC", _market.ResourcesText);
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
        Assert.Equal(["Base Market", "Number One", "Medical Services", "Additional Recruitment"], round.Marketplace.Stores);
        Assert.Equal(0, Item("Pistol").Quantity);
        Assert.False(_market.HasCartItems);
        Assert.Equal(1, _purchases);
    }

    // Number One selling a Chemtank, whose copies each roll on the Metachemistry table as they are bought.
    private void SellChemtanks()
    {
        var store = new Store
        {
            Name = "Number One",
            AssociatedFactions = ["PanOceania"],
            Alignment = StoreAlignments.Lawful,
            TroopTypes = [new StoreTroopType { ArmorName = "Chemtank", CostCr = 5, Effects = new ArmorEffects { Roll = RecruitRollTable.Metachemistry } }]
        };
        _kit.Stores.GetAvailableStoresAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns([SeasonTestKit.Recruitment, SeasonTestKit.BaseMarket, store, SeasonTestKit.Medical]);
        _kit.Stores.GetStoreByNameAsync("Number One", Arg.Any<CancellationToken>()).Returns(store);
        _kit.ArmyData.GetRecruitTable(RecruitRollTable.Metachemistry).Returns([new RollRange(1, 10, "Regeneration"), new RollRange(11, 20, "+6 BTS")]);
    }

    [Fact]
    public async Task EachCopyOfRolledArmourKeepsItsOwnRoll()
    {
        SellChemtanks();
        GiveThreeRounds();
        await OpenAsync();
        _kit.Dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns(RecruitRollPrompt.RollChoice);
        _kit.Dice.RollD20().Returns(4, 15);
        _market.IncrementCommand.Execute(_market.TroopTypeRows[0]);
        _market.IncrementCommand.Execute(_market.TroopTypeRows[0]);

        await _market.BuyCommand.ExecuteAsync(null);

        Assert.Equal(["Regeneration", "+6 BTS"], _kit.Season.Rounds[2].Marketplace.Transactions.Select(transaction => transaction.RolledResult));
    }

    [Fact]
    public async Task PuttingOffArmoursRollBuysNothing()
    {
        SellChemtanks();
        GiveThreeRounds();
        await OpenAsync();
        _market.IncrementCommand.Execute(_market.TroopTypeRows[0]);

        await _market.BuyCommand.ExecuteAsync(null);

        Assert.Empty(_kit.Season.Rounds[2].Marketplace.Transactions);
        Assert.True(_market.HasCartItems);
        Assert.Equal(0, _purchases);
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
    public async Task BuyingBeforeTheFirstContractRecordsTheInitialPurchases()
    {
        _kit.Season = new SeasonFile { StartingFunds = new SeasonStartingFunds { Credits = 20 } };
        await OpenAsync();
        _market.IncrementCommand.Execute(Item("Pistol"));

        await _market.BuyCommand.ExecuteAsync(null);

        Assert.Equal("Pistol", _kit.Season.InitialPurchases.Transactions.Single().ItemName);
        Assert.Equal(["Base Market", "Number One", "Medical Services", "Additional Recruitment"], _kit.Season.InitialPurchases.Stores);
        Assert.Equal(1, _purchases);
    }

    [Fact]
    public async Task AFailedSaveSaysSoAndKeepsTheCart()
    {
        GiveThreeRounds();
        await OpenAsync();
        _market.IncrementCommand.Execute(Item("Pistol"));
        _kit.SeasonFiles.UpdateAsync(SeasonTestKit.SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(false);

        await _market.BuyCommand.ExecuteAsync(null);

        await _kit.Dialogs.Received(1).ShowAlertAsync("Nothing Bought", SeasonMessages.NotSaved);
        Assert.True(_market.HasCartItems);
        Assert.Equal(0, _purchases);
    }

    [Fact]
    public async Task ALaterPickWinsOverASlowerStore()
    {
        var slow = new TaskCompletionSource<Store?>();
        await OpenAsync("Jade Temu");
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
