using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Loot;

namespace InfinityMercsApp.UnitTests.Season.Loot;

public sealed class SeasonLootTests
{
    private const string Loot = "Spitfire [Shock]";

    private static readonly SeasonFile Season = new()
    {
        LootBoxes =
        [
            new SeasonLootBox
            {
                Tier = LootBoxTier.BlackMarketCache,
                GainedAfterRound = 2,
                IsOpened = true,
                Weapon = new SeasonLootRoll { Dice = 4, Result = 21 },
                Buffs = [new SeasonLootRoll { Dice = 1, Result = 1 }]
            },
            new SeasonLootBox { Tier = LootBoxTier.ArtifactVault, Weapon = new SeasonLootRoll { Dice = 1, Result = 2 } }
        ]
    };

    [Fact]
    public void Items_OpenedBoxesOnly_AreListed() => Assert.Equal([Loot], SeasonLoot.Items(Season).Select(item => item.Name));

    [Fact]
    public void Catalog_Loot_IsKnownWithItsRenownAndSwc()
    {
        var item = SeasonLoot.Catalog(InventoryCatalog.Empty, Season).Find(Loot);

        Assert.NotNull(item);
        Assert.Equal(("Primary", LootItem.StoreName, 15, 1.5m), (item.Category, item.StoreName, item.CostCr, item.CostSwc));
    }

    [Fact]
    public void SeasonInventoryFrom_Loot_IsOwnedButCannotBeSold()
    {
        var entry = Assert.Single(SeasonInventory.From(Season, InventoryCatalog.Empty).Entries);

        Assert.Equal((Loot, "Primary", 0), (entry.Name, entry.Category, entry.SellPrice));
        Assert.Equal(["Loot Box: Black Market Cache"], entry.Sources);
        Assert.Equal([2], entry.Rounds);
    }
}
