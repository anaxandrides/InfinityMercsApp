using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Presentation.Season.Overview;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class WarMarketChoiceViewModelTests
{
    private readonly SeasonTestKit _kit = new();
    private readonly WarMarketChoiceViewModel _choice;
    private readonly List<string> _chosen = [];

    public WarMarketChoiceViewModelTests()
    {
        _choice = new WarMarketChoiceViewModel(_kit.SeasonFiles, _kit.Dice, store =>
        {
            _chosen.Add(store);
            return Task.CompletedTask;
        });
    }

    // The company's own market is Number One.
    private void Open(SeasonFile? season = null)
    {
        _choice.Prepare(SeasonTestKit.SeasonPath, season ?? _kit.Season, ["Number One"], ["Number One", "Jade Temu", "Arachne Req"]);
        _choice.OpenCommand.Execute(null);
    }

    [Fact]
    public void PreparingLeavesThePopupShut()
    {
        _choice.Prepare(SeasonTestKit.SeasonPath, _kit.Season, ["Number One"], ["Number One", "Jade Temu"]);

        Assert.True(_choice.IsNeeded);
        Assert.False(_choice.IsOpen);
    }

    [Fact]
    public void OpensForARoundWithoutAWarMarket()
    {
        Open();

        Assert.True(_choice.IsOpen);
        Assert.True(_choice.CanRoll);
        Assert.Equal(["Number One"], _choice.FactionMarketChoices);
        Assert.True(_choice.HasFactionMarkets);
        Assert.Equal(WarMarketChoiceViewModel.NotRolled, _choice.ResultText);
        Assert.Equal(["Jade Temu", "Arachne Req"], _choice.Choices);
        Assert.False(_choice.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void StaysShutOnceTheRoundHasOne()
    {
        _kit.Season.Rounds[0].Marketplace.WarMarket = "Jade Temu";

        Open();

        Assert.False(_choice.IsNeeded);
        Assert.False(_choice.IsOpen);
    }

    [Fact]
    public void ARoundsOldTemporaryStoreCountsAsItsWarMarket()
    {
        _kit.Season.Rounds[0].TemporaryStore = "Jade Temu";

        Open();

        Assert.False(_choice.IsOpen);
    }

    [Fact]
    public void WithoutASeasonNothingIsNeeded()
    {
        _choice.Prepare(SeasonTestKit.SeasonPath, null, [], []);
        _choice.OpenCommand.Execute(null);

        Assert.False(_choice.IsNeeded);
        Assert.False(_choice.IsOpen);
    }

    [Fact]
    public void BeforeTheFirstContractOnlyAFactionMarketCanBeTaken()
    {
        Open(new SeasonFile());
        _kit.Dice.RollD20().Returns(3);

        Assert.True(_choice.IsOpen);
        Assert.False(_choice.CanRoll);

        _choice.RollCommand.Execute(null);
        _choice.SelectedRoll = _choice.RollOptions[2];

        Assert.Equal(WarMarketChoiceViewModel.NotRolled, _choice.ResultText);
        Assert.Null(_choice.PendingStore);
        _kit.Dice.DidNotReceive().RollD20();
    }

    [Fact]
    public void TakingTheFactionsMarket()
    {
        Open();

        _choice.SelectedFactionMarket = "Number One";

        Assert.Equal("Taking Number One", _choice.ResultText);
        Assert.Equal(StoreSearchTone.Found, _choice.ResultTone);
        Assert.Equal("Number One", _choice.PendingStore);
    }

    [Fact]
    public void RollingRerollsTheCompanysOwnMarkets()
    {
        Open();
        _choice.SelectedFactionMarket = "Number One";
        _kit.Dice.RollD20().Returns(1, 2, 3);

        _choice.RollCommand.Execute(null);

        Assert.Equal("Rolled 3 — Jade Temu", _choice.ResultText);
        Assert.Equal(StoreSearchTone.Found, _choice.ResultTone);
        Assert.Equal("Jade Temu", _choice.PendingStore);
        Assert.Null(_choice.SelectedFactionMarket);
        Assert.True(_choice.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void ANineteenLetsTheCompanyChoose()
    {
        Open();
        _kit.Dice.RollD20().Returns(19);

        _choice.RollCommand.Execute(null);
        Assert.Equal("Rolled 19 — choose any market", _choice.ResultText);
        Assert.True(_choice.IsChoosingAny);
        Assert.Null(_choice.PendingStore);

        _choice.SelectedChoice = "Arachne Req";
        Assert.Equal("Arachne Req", _choice.PendingStore);
    }

    [Fact]
    public void RollingOnlyTheCompanysMarketsEndsInAChoice()
    {
        Open();
        _kit.Dice.RollD20().Returns(1);

        _choice.RollCommand.Execute(null);

        Assert.Equal(StoreSearchTone.Choose, _choice.ResultTone);
        Assert.True(_choice.IsChoosingAny);
    }

    [Fact]
    public void PickingTheNumberRolledAtTheTable()
    {
        Open();

        _choice.SelectedRoll = _choice.RollOptions[4];
        Assert.Equal("5 — Arachne Req", _choice.ResultText);
        Assert.Equal("Arachne Req", _choice.PendingStore);

        _choice.SelectedRoll = _choice.RollOptions[0];
        Assert.Equal("1 — Number One (your own market — roll again)", _choice.ResultText);
        Assert.Equal(StoreSearchTone.Problem, _choice.ResultTone);
        Assert.Null(_choice.PendingStore);

        _choice.SelectedRoll = _choice.RollOptions[^1];
        Assert.Equal("Picked 19-20 — choose any market", _choice.ResultText);
        Assert.True(_choice.IsChoosingAny);
    }

    [Fact]
    public void AChoiceOnlyCountsWhenChoosingIsAllowed()
    {
        Open();

        _choice.SelectedChoice = "Jade Temu";

        Assert.Null(_choice.PendingStore);
    }

    [Fact]
    public void ClosingPutsTheChoiceOff()
    {
        Open();

        _choice.CloseCommand.Execute(null);

        Assert.False(_choice.IsOpen);
        Assert.True(_choice.IsNeeded);
    }

    [Fact]
    public void OpeningAgainStartsAfresh()
    {
        Open();
        _choice.SelectedRoll = _choice.RollOptions[2];
        _choice.CloseCommand.Execute(null);

        _choice.OpenCommand.Execute(null);

        Assert.Equal(WarMarketChoiceViewModel.NotRolled, _choice.ResultText);
        Assert.Null(_choice.PendingStore);
        Assert.Null(_choice.SelectedRoll);
    }

    [Fact]
    public async Task ConfirmingSavesTheMarketForTheRoundAndCloses()
    {
        Open();
        _choice.SelectedRoll = _choice.RollOptions[2];

        await _choice.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("Jade Temu", _kit.Season.Rounds[0].Marketplace.WarMarket);
        Assert.False(_choice.IsOpen);
        Assert.False(_choice.IsNeeded);
        Assert.Equal(["Jade Temu"], _chosen);
    }

    [Fact]
    public async Task BeforeTheFirstContractTheMarketIsSavedWithTheInitialPurchases()
    {
        _kit.Season = new SeasonFile();
        Open();
        _choice.SelectedFactionMarket = "Number One";

        await _choice.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("Number One", _kit.Season.InitialPurchases.WarMarket);
    }

    [Fact]
    public void TheRollOptionsCoverTheTable()
    {
        Assert.Equal(19, StoreRollOption.All.Count);
        Assert.Equal("1 — Number One", StoreRollOption.All[0].Label);
        Assert.Equal("19-20 — Choose any", StoreRollOption.All[^1].Label);
    }
}
