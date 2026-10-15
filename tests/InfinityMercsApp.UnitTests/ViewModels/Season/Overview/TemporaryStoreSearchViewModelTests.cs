using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class TemporaryStoreSearchViewModelTests
{
    private readonly SeasonTestKit _kit = new();
    private readonly TemporaryStoreSearchViewModel _search;
    private readonly List<string> _chosen = [];

    public TemporaryStoreSearchViewModelTests()
    {
        _search = new TemporaryStoreSearchViewModel(_kit.SeasonFiles, _kit.Dice, store =>
        {
            _chosen.Add(store);
            return Task.CompletedTask;
        });
    }

    private void Open() =>
        _search.OpenIfNeeded(SeasonTestKit.SeasonPath, _kit.Season, ["Number One"], ["Number One", "Jade Temu", "Arachne Req"]);

    [Fact]
    public void OpensForARoundWithoutATemporaryStore()
    {
        Open();

        Assert.True(_search.IsOpen);
        Assert.Equal(TemporaryStoreSearchViewModel.NotRolled, _search.ResultText);
        Assert.Equal(["Jade Temu", "Arachne Req"], _search.Choices);
        Assert.False(_search.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void StaysShutWhenTheRoundHasOneOrThereIsNoRound()
    {
        _kit.Season.Rounds[0].TemporaryStore = "Jade Temu";
        Open();
        Assert.False(_search.IsOpen);

        _search.OpenIfNeeded(SeasonTestKit.SeasonPath, new SeasonFile(), [], []);
        Assert.False(_search.IsOpen);
    }

    [Fact]
    public void RollingRerollsStoresTheCompanyHas()
    {
        Open();
        _kit.Dice.RollD20().Returns(1, 2, 3);

        _search.RollCommand.Execute(null);

        Assert.Equal("Rolled 3 — Jade Temu", _search.ResultText);
        Assert.Equal(StoreSearchTone.Found, _search.ResultTone);
        Assert.Equal("Jade Temu", _search.PendingStore);
        Assert.True(_search.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public void ANineteenLetsTheCompanyChoose()
    {
        Open();
        _kit.Dice.RollD20().Returns(19);

        _search.RollCommand.Execute(null);
        Assert.Equal("Rolled 19 — choose any store", _search.ResultText);
        Assert.True(_search.IsChoosingAny);
        Assert.Null(_search.PendingStore);

        _search.SelectedChoice = "Arachne Req";
        Assert.Equal("Arachne Req", _search.PendingStore);
    }

    [Fact]
    public void RollingOnlyStoresTheCompanyHasEndsInAChoice()
    {
        Open();
        _kit.Dice.RollD20().Returns(1);

        _search.RollCommand.Execute(null);

        Assert.Equal(StoreSearchTone.Choose, _search.ResultTone);
        Assert.True(_search.IsChoosingAny);
    }

    [Fact]
    public void PickingTheNumberRolledAtTheTable()
    {
        Open();

        _search.SelectedRoll = _search.RollOptions[4];
        Assert.Equal("5 — Arachne Req", _search.ResultText);
        Assert.Equal("Arachne Req", _search.PendingStore);

        _search.SelectedRoll = _search.RollOptions[0];
        Assert.Equal("1 — Number One (already owned — pick another)", _search.ResultText);
        Assert.Equal(StoreSearchTone.Problem, _search.ResultTone);
        Assert.Null(_search.PendingStore);

        _search.SelectedRoll = _search.RollOptions[^1];
        Assert.Equal("Picked 19-20 — choose any store", _search.ResultText);
        Assert.True(_search.IsChoosingAny);
    }

    [Fact]
    public void AChoiceOnlyCountsWhenChoosingIsAllowed()
    {
        Open();

        _search.SelectedChoice = "Jade Temu";

        Assert.Null(_search.PendingStore);
    }

    [Fact]
    public async Task ConfirmingSavesTheStoreAndCloses()
    {
        Open();
        _search.SelectedRoll = _search.RollOptions[2];

        await _search.ConfirmCommand.ExecuteAsync(null);

        Assert.Equal("Jade Temu", _kit.Season.Rounds[0].TemporaryStore);
        Assert.False(_search.IsOpen);
        Assert.Equal(["Jade Temu"], _chosen);
    }

    [Fact]
    public void TheRollOptionsCoverTheTable()
    {
        Assert.Equal(19, StoreRollOption.All.Count);
        Assert.Equal("1 — Number One", StoreRollOption.All[0].Label);
        Assert.Equal("19-20 — Choose any", StoreRollOption.All[^1].Label);
    }
}
