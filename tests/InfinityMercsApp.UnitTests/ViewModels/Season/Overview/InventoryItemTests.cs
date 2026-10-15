using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.ViewModels.Season.Overview;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class InventoryItemTests
{
    [Fact]
    public void SeveralRoundsAreListed()
    {
        var item = new InventoryItem(new InventoryEntry("Pistol", "Sidearm", 3, 2, ["Number One", "Jade Temu"], [0, 2]));

        Assert.Equal("Bought: Rounds 0, 2", item.RoundsText);
        Assert.Equal("Number One, Jade Temu", item.SourcesText);
        Assert.True(item.HasSources && item.HasRounds && item.CanSell);
    }

    [Fact]
    public void NothingKnownShowsNothing()
    {
        var item = new InventoryItem(new InventoryEntry("Trophy", "General", 0, 1, [], []));

        Assert.Equal(string.Empty, item.RoundsText);
        Assert.False(item.HasSources || item.HasRounds || item.CanSell);
    }
}
