using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Presentation.Season.Overview;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class SeasonInventoryViewModelTests
{
    private readonly SeasonTestKit _kit = new();
    private readonly SeasonInventoryViewModel _inventory;
    private int _changes;

    public SeasonInventoryViewModelTests()
    {
        _inventory = new SeasonInventoryViewModel(_kit.SeasonFiles, _kit.Dialogs, () =>
        {
            _changes++;
            return Task.CompletedTask;
        });
    }

    private static readonly InventoryCatalog Catalog = InventoryCatalog.From([SeasonTestKit.NumberOne, SeasonTestKit.JadeTemu]);

    private void ShowOwned(params IEnumerable<SeasonTransaction> transactions)
    {
        _kit.Season.Rounds[0].Marketplace.Transactions.AddRange(transactions);
        _inventory.Show(SeasonTestKit.SeasonPath, _kit.Season, SeasonInventory.From(_kit.Season, Catalog));
    }

    private void ConfirmSales(bool confirm) =>
        _kit.Dialogs.ConfirmAsync("Sell Equipment", Arg.Any<string>(), "Sell", "Cancel").Returns(confirm);

    [Fact]
    public void Show_OwnedGear_ListsItByCategory()
    {
        ShowOwned(SeasonTestKit.Bought("Number One", "Pistol"), SeasonTestKit.Bought("Number One", "Pistol"), SeasonTestKit.Bought("Number One", "Combi Rifle"));

        Assert.Equal(["PRIMARY", "SIDEARM"], _inventory.Sections.Select(section => section.Title));
        var pistol = _inventory.Sections[1].Items.Single();
        Assert.Equal(("Pistol", "x2", "Number One", "Bought: Round 1", "SELL (2cr)"), (pistol.Name, pistol.CountText, pistol.SourcesText, pistol.RoundsText, pistol.SellText));
        Assert.False(_inventory.HasMessage);
    }

    [Fact]
    public void Show_NothingOwned_SaysSo()
    {
        ShowOwned();

        Assert.True(_inventory.HasMessage);
        Assert.Equal(SeasonInventoryViewModel.EmptyMessage, _inventory.Message);
    }

    [Fact]
    public void Show_NoSeason_SaysSo()
    {
        _inventory.Show(null, null, SeasonInventory.Empty);

        Assert.Equal(SeasonInventoryViewModel.NoSeasonMessage, _inventory.Message);
    }

    [Fact]
    public async Task SellCommand_Confirmed_RecordsASaleForHalfThePrice()
    {
        ShowOwned(SeasonTestKit.Bought("Number One", "Combi Rifle"));
        ConfirmSales(true);

        await _inventory.SellCommand.ExecuteAsync(_inventory.Sections[0].Items[0]);

        var sale = _kit.Season.Rounds[0].Marketplace.Transactions.Last();
        Assert.Equal(("Number One", "Combi Rifle", -6, true), (sale.OriginStore, sale.ItemName, sale.CostCr, sale.IsSale));
        await _kit.Dialogs.Received(1).ConfirmAsync("Sell Equipment", "Sell Combi Rifle for 6 CR?", "Sell", "Cancel");
        Assert.Equal(1, _changes);
    }

    [Fact]
    public async Task SellCommand_Cancelled_SellsNothing()
    {
        ShowOwned(SeasonTestKit.Bought("Number One", "Combi Rifle"));
        ConfirmSales(false);

        await _inventory.SellCommand.ExecuteAsync(_inventory.Sections[0].Items[0]);

        Assert.Single(_kit.Season.Rounds[0].Marketplace.Transactions);
        Assert.Equal(0, _changes);
    }

    [Fact]
    public async Task SellCommand_UnpricedItem_SellsNothing()
    {
        var unpriced = new InventoryItem(new InventoryEntry("Trophy", "General", 0, 1, [], []));

        await _inventory.SellCommand.ExecuteAsync(unpriced);

        Assert.False(unpriced.CanSell);
        await _kit.Dialogs.DidNotReceiveWithAnyArgs().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SellCommand_BeforeFirstContract_RecordsWithInitialPurchases()
    {
        _kit.Season = new SeasonFile();
        _kit.Season.InitialPurchases.Transactions.Add(SeasonTestKit.Bought("Number One", "Combi Rifle"));
        _inventory.Show(SeasonTestKit.SeasonPath, _kit.Season, SeasonInventory.From(_kit.Season, Catalog));
        ConfirmSales(true);

        await _inventory.SellCommand.ExecuteAsync(_inventory.Sections[0].Items[0]);

        Assert.True(_kit.Season.InitialPurchases.Transactions.Last().IsSale);
        Assert.Equal(1, _changes);
    }

    [Fact]
    public async Task SellCommand_SaveFails_SaysSo()
    {
        ShowOwned(SeasonTestKit.Bought("Number One", "Combi Rifle"));
        _kit.SeasonFiles.UpdateAsync(SeasonTestKit.SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(false);
        ConfirmSales(true);

        await _inventory.SellCommand.ExecuteAsync(_inventory.Sections[0].Items[0]);

        await _kit.Dialogs.Received(1).ShowAlertAsync("Nothing Sold", SeasonMessages.NotSaved);
        Assert.Equal(0, _changes);
    }
}
