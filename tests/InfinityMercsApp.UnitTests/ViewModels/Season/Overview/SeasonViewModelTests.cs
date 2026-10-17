using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Season;
using InfinityMercsApp.ViewModels.Season.Overview;
using InfinityMercsApp.ViewModels.Season.Overview.Items;
using InfinityMercsApp.Services.Season.Peripherals;
using InfinityMercsApp.UnitTests.Services.Season.Peripherals;
using InfinityMercsApp.UnitTests.ViewModels.Company.Viewer;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
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
        Assert.False(viewModel.WarMarketChoice.IsOpen);
        await _kit.SeasonFiles.DidNotReceiveWithAnyArgs().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TheCompanysRenownCountsGearXpAndWins()
    {
        _kit.Season.Rounds[0].MissionResults = new SeasonMissionResult { Won = true, UnitResults = [new() { EntryIndex = 1, XpGained = 5 }] };
        _kit.Season.UnitGear[1] = [new SeasonUnitGear { ItemName = "Knife", Slot = GearSlot.Sidearm.Key() }];

        var viewModel = await _kit.OpenAsync();

        // Two troopers at 20 each (the peripheral is left out), the Knife's 2 CR, Bolt's 5 XP and one win.
        Assert.Equal(20 + 20 + 2 + 5 + 10, viewModel.Renown);
    }

    [Fact]
    public async Task ACompanyWithoutASeasonStartsOne()
    {
        _kit.Season = new SeasonFile();

        var viewModel = await _kit.OpenAsync(withSeasonFile: false);

        await _kit.SeasonFiles.Received(1).CreateAsync("Blue Team", "blue-1", SeasonTestKit.CompanyPath);
        Assert.Equal("Round 0", viewModel.RoundTitle);
        Assert.False(viewModel.WarMarketChoice.IsOpen);
    }

    // Bolt has Booty, and the Booty table gives a DA CC Weapon on 7.
    private SavedCompanyFile BoltWithBooty()
    {
        var company = SeasonTestKit.Company();
        company.Entries[0].CustomSkills.Add("Booty");
        _kit.CompanyFiles.LoadAsync(SeasonTestKit.CompanyPath).Returns(company);
        _kit.ArmyData.GetRecruitTable(RecruitRollTable.Booty).Returns([new RollRange(1, 10, "DA CC Weapon"), new RollRange(11, 20, "HMG")]);
        _kit.Dialogs.ChooseAsync(Arg.Any<string>(), "Later", Arg.Any<IReadOnlyList<string>>()).Returns(RecruitRollPrompt.RollChoice);
        _kit.Dice.RollD20().Returns(7);
        return company;
    }

    [Fact]
    public async Task AFoundedCompanyRollsForBootyAndKeepsTheResult()
    {
        _kit.Season = new SeasonFile();
        var company = BoltWithBooty();

        var viewModel = await _kit.OpenAsync(withSeasonFile: false);

        Assert.Equal([new RecruitRoll(RecruitRollTable.Booty, 7, "DA CC Weapon")], company.Entries[0].RecruitRolls);
        await _kit.CompanyFiles.Received(1).SaveAsync(SeasonTestKit.CompanyPath, company);
        Assert.Empty(viewModel.Team.Units.Single(unit => unit.Name == "Bolt").OwedRolls);
    }

    // A company opened again is not asked; the Team tab offers the roll instead.
    [Fact]
    public async Task OpeningASeasonDoesNotAskForRollsOwedFromBefore()
    {
        BoltWithBooty();

        var viewModel = await _kit.OpenAsync();

        await _kit.Dialogs.DidNotReceiveWithAnyArgs().ChooseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
        Assert.NotEmpty(viewModel.Team.Units.Single(unit => unit.Name == "Bolt").OwedRolls);
    }

    [Fact]
    public async Task ANewRecruitRollsWhenThePageIsBack()
    {
        var viewModel = await _kit.OpenAsync();
        var company = BoltWithBooty();
        var recruit = CompanyRosterTests.Entry(4, "Ghulam", skills: ["Booty"]);
        company.Entries.Add(recruit);

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        Assert.Single(recruit.RecruitRolls);
        Assert.Empty(company.Entries[0].RecruitRolls);
    }

    [Fact]
    public async Task PuttingARollOffSavesNothing()
    {
        _kit.Season = new SeasonFile();
        var company = BoltWithBooty();
        _kit.Dialogs.ChooseAsync(Arg.Any<string>(), "Later", Arg.Any<IReadOnlyList<string>>()).Returns((string?)null);

        await _kit.OpenAsync(withSeasonFile: false);

        Assert.Empty(company.Entries[0].RecruitRolls);
        await _kit.CompanyFiles.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<string>(), Arg.Any<SavedCompanyFile>());
    }

    // The Tohaa army is known, the season has 20 CR, and Bolt is picking a peripheral.
    private async Task<(SeasonViewModel ViewModel, SavedCompanyFile Company)> PickingAPeripheralAsync(string? choice)
    {
        TohaaArmyData.AddTo(_kit.ArmyData);
        _kit.Season.StartingFunds = new SeasonStartingFunds { Credits = 20 };
        var company = SeasonTestKit.Company();
        _kit.CompanyFiles.LoadAsync(SeasonTestKit.CompanyPath).Returns(company);
        var viewModel = await _kit.OpenAsync();
        _kit.Dialogs.ChooseAsync("Peripheral for Bolt", "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns(choice);
        return (viewModel, company);
    }

    private static PeripheralChange BoltGainsControl(params IEnumerable<int> losing) => new([.. losing], 1, "Bolt");

    [Fact]
    public async Task BuyingAPeripheralAddsItToTheRosterAndChargesItsCost()
    {
        var (viewModel, company) = await PickingAPeripheralAsync("Chaksa Peripheral B: Combi Rifle (Gorgos Squad), 4 CR");

        Assert.True(await ((ITeamChanges)viewModel).ChangePeripheralsAsync(BoltGainsControl()));

        var peripheral = company.Entries.Single(entry => entry.FromControllerAugment);
        Assert.Equal((4, 1), (peripheral.EntryIndex, peripheral.ParentEntryIndex));
        await _kit.CompanyFiles.Received().SaveAsync(SeasonTestKit.CompanyPath, company);
        var charge = SeasonRounds.CurrentMarketplace(_kit.Season).Transactions.Single();
        Assert.Equal((SeasonViewModel.PeripheralPrefix + "Chaksa Peripheral B", 4), (charge.ItemName, charge.CostCr));
    }

    [Fact]
    public async Task AnOwnedPeripheralIsOfferedAgainForFree()
    {
        var (viewModel, company) = await PickingAPeripheralAsync("Chaksa Peripheral B (owned, #7)");
        company.Entries.Add(new SavedCompanyEntry { EntryIndex = 7, Name = "Chaksa Peripheral B", IsPeripheralUnit = true, FromControllerAugment = true, ParentEntryIndex = 2 });

        // The lieutenant's augment moves to Bolt, and its peripheral with it.
        Assert.True(await ((ITeamChanges)viewModel).ChangePeripheralsAsync(BoltGainsControl(2)));

        Assert.Equal(1, company.Entries.Single(entry => entry.EntryIndex == 7).ParentEntryIndex);
        Assert.Empty(SeasonRounds.CurrentMarketplace(_kit.Season).Transactions);
    }

    [Fact]
    public async Task APeripheralTheCompanyCantAffordIsNotBought()
    {
        var (viewModel, company) = await PickingAPeripheralAsync("Chaksa Peripheral A: Combi Rifle (Gorgos Squad), 7 CR");
        // Before the first contract, with only the 5 CR left from recruiting.
        _kit.Season.Rounds.Clear();
        _kit.Season.StartingFunds = new SeasonStartingFunds { Credits = 5 };

        Assert.False(await ((ITeamChanges)viewModel).ChangePeripheralsAsync(BoltGainsControl()));

        Assert.DoesNotContain(company.Entries, entry => entry.FromControllerAugment);
        await _kit.Dialogs.Received(1).ShowAlertAsync("Not Enough CR", Arg.Any<string>());
    }

    [Fact]
    public async Task CancellingThePickChangesNothing()
    {
        var (viewModel, company) = await PickingAPeripheralAsync(null);
        company.Entries.Add(new SavedCompanyEntry { EntryIndex = 7, Name = "Chaksa Peripheral B", IsPeripheralUnit = true, FromControllerAugment = true, ParentEntryIndex = 2 });

        Assert.False(await ((ITeamChanges)viewModel).ChangePeripheralsAsync(BoltGainsControl(2)));

        await _kit.CompanyFiles.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<string>(), Arg.Any<SavedCompanyFile>());
    }

    [Fact]
    public async Task SellingAPeripheralRemovesItForHalfItsCost()
    {
        var (viewModel, company) = await PickingAPeripheralAsync(null);
        company.Entries.Add(new SavedCompanyEntry { EntryIndex = 7, Name = "Chaksa Peripheral A", IsPeripheralUnit = true, FromControllerAugment = true, Cost = 7 });
        _kit.Dialogs.ConfirmAsync("Sell Peripheral", Arg.Any<string>(), "Sell", "Cancel").Returns(true);

        await ((ITeamChanges)viewModel).SellPeripheralAsync(7);

        Assert.DoesNotContain(company.Entries, entry => entry.EntryIndex == 7);
        var sale = SeasonRounds.CurrentMarketplace(_kit.Season).Transactions.Single();
        Assert.Equal(-4, sale.CostCr);
        Assert.True(sale.IsSale);
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
    public async Task TheCompanyAndSeasonAreReadAgainOnEachAppearance()
    {
        var viewModel = await _kit.OpenAsync();
        _kit.Season.Rounds.Add(new SeasonRound { RoundIndex = 2 });
        var recruited = SeasonTestKit.Company();
        recruited.Entries.Add(CompanyRosterTests.Entry(4, "Recruit"));
        _kit.CompanyFiles.LoadAsync(SeasonTestKit.CompanyPath).Returns(recruited);

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        await _kit.CompanyFiles.Received(2).LoadAsync(SeasonTestKit.CompanyPath);
        Assert.Equal("Round 2 Marketplace", viewModel.RoundTitle);
        Assert.Contains("Recruit", viewModel.Team.Units.Select(unit => unit.Name));
    }

    [Fact]
    public async Task ARoundWithoutAWarMarketAsksForOneOnTheMarketTab()
    {
        var panOceania = new Faction { Id = 101, ParentId = 101, Name = "PanOceania" };
        _kit.ArmyData.GetMetadataFactionById(101).Returns(panOceania);
        _kit.ArmyData.GetMetadataFactions().Returns([panOceania]);
        var viewModel = await _kit.OpenAsync();
        Assert.True(viewModel.WarMarketChoice.IsNeeded);
        Assert.False(viewModel.WarMarketChoice.IsOpen);

        await viewModel.ShowTabCommand.ExecuteAsync(SeasonTab.Marketplace);

        Assert.True(viewModel.WarMarketChoice.IsOpen);
        Assert.Equal(["Number One"], viewModel.WarMarketChoice.FactionMarketChoices);
        Assert.Equal(["Jade Temu"], viewModel.WarMarketChoice.Choices);
    }

    [Fact]
    public async Task TheMarketTabDoesNotAskOnceAWarMarketIsChosen()
    {
        _kit.Season.Rounds[0].Marketplace.WarMarket = "Number One";
        var viewModel = await _kit.OpenAsync();

        await viewModel.ShowTabCommand.ExecuteAsync(SeasonTab.Marketplace);

        Assert.False(viewModel.WarMarketChoice.IsNeeded);
        Assert.False(viewModel.WarMarketChoice.IsOpen);
    }

    [Fact]
    public async Task ChoosingAWarMarketAddsItToTheMarkets()
    {
        var viewModel = await _kit.OpenAsync();
        await viewModel.ShowTabCommand.ExecuteAsync(SeasonTab.Marketplace);
        _kit.Dice.RollD20().Returns(3);
        viewModel.WarMarketChoice.RollCommand.Execute(null);

        await viewModel.WarMarketChoice.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("Jade Temu", _kit.Season.Rounds[0].Marketplace.WarMarket);
        Assert.False(viewModel.WarMarketChoice.IsOpen);
        Assert.Contains(viewModel.Marketplace.Stores, store => store.Label == "Jade Temu [YJ, Chaotic]");
    }

    [Fact]
    public async Task ASeasonWithoutStartingFundsTakesThemFromTheCompany()
    {
        _kit.CompanyFiles.LoadAsync(SeasonTestKit.CompanyPath).Returns(new SavedCompanyFile
        {
            CompanyName = "Blue Team",
            StartSeasonPoints = 75,
            CurrentPoints = 60,
            Entries = SeasonTestKit.Company().Entries
        });

        var viewModel = await _kit.OpenAsync();

        Assert.Equal(15, _kit.Season.StartingFunds?.Credits);
        Assert.Equal("25 CR - 0.5 SWC", viewModel.Marketplace.ResourcesText);
    }

    [Fact]
    public async Task StartingFundsAreOnlyWorkedOutOnce()
    {
        // Before the first contract, with only the 5 CR left from recruiting.
        _kit.Season.Rounds.Clear();
        _kit.Season.StartingFunds = new SeasonStartingFunds { Credits = 5 };

        await _kit.OpenAsync();

        Assert.Equal(5, _kit.Season.StartingFunds.Credits);
        await _kit.SeasonFiles.DidNotReceive().UpdateAsync(Arg.Any<string?>(), Arg.Any<Action<SeasonFile>>());
    }

    [Fact]
    public async Task TheTeamTabKnowsTheCompanysSwc()
    {
        var viewModel = await _kit.OpenAsync();

        Assert.Equal("SWC EQUIPPED 0 / 0.5", viewModel.Team.SwcText);
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

        Assert.Equal("Knife", Assert.Single(viewModel.Inventory.Sections).Items[0].Name);
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
    public async Task TakingAByeOpensTheByeRoundPage()
    {
        var viewModel = await _kit.OpenAsync();

        await viewModel.TakeByeCommand.ExecuteAsync(null);

        await _kit.Navigation.Received(1).PushAsync("ByeRoundPage", Arg.Is<IDictionary<string, object>>(parameters =>
            (string)parameters[NavigationParameters.CompanyFilePath] == SeasonTestKit.CompanyPath &&
            (string)parameters[NavigationParameters.SeasonFilePath] == SeasonTestKit.SeasonPath));
    }

    [Fact]
    public async Task NavigatingWithoutACompanyChangesNothing()
    {
        var viewModel = _kit.SeasonViewModel();
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>());

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        await _kit.CompanyFiles.DidNotReceiveWithAnyArgs().LoadAsync(Arg.Any<string>());
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
    public async Task AdditionalRecruitmentOffersRecruitingInsteadOfStock()
    {
        var viewModel = await _kit.OpenAsync();

        viewModel.Marketplace.SelectedStore = viewModel.Marketplace.Stores.Single(store => store.StoreName == ServiceStoreNames.AdditionalRecruitment);

        Assert.True(viewModel.Marketplace.IsRecruitmentStore);
        Assert.False(viewModel.Marketplace.IsServicesStore);
        Assert.False(viewModel.Marketplace.ShowsStock);
    }

    [Fact]
    public async Task RecruitingOpensTheCompanysScreenWithTheCrLeft()
    {
        _kit.CompanySelection.OpenRecruitmentAsync(Arg.Any<CompanyRecruitment>()).Returns(true);
        var viewModel = await _kit.OpenAsync();

        await viewModel.RecruitCommand.ExecuteAsync(null);

        var credits = SeasonEconomy.Ledger(_kit.Season).CreditsBalance;
        await _kit.CompanySelection.Received(1).OpenRecruitmentAsync(Arg.Is<CompanyRecruitment>(recruitment =>
            recruitment.CompanyFilePath == SeasonTestKit.CompanyPath &&
            recruitment.SeasonFilePath == SeasonTestKit.SeasonPath &&
            recruitment.Company.CompanyName == "Blue Team" &&
            recruitment.Credits == credits));
        await _kit.Dialogs.DidNotReceiveWithAnyArgs().ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ACompanyTypeWithoutARecruitmentScreenSaysSo()
    {
        _kit.CompanySelection.OpenRecruitmentAsync(Arg.Any<CompanyRecruitment>()).Returns(false);
        var viewModel = await _kit.OpenAsync();

        await viewModel.RecruitCommand.ExecuteAsync(null);

        await _kit.Dialogs.Received(1).ShowAlertAsync("Can't Recruit", "There is no recruitment screen for a Standard Company.");
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
