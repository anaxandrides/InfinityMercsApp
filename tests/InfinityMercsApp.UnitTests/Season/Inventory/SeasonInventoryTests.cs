using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.UnitTests.Season.Inventory;

public sealed class SeasonInventoryTests
{
    private static readonly InventoryCatalog Catalog =
        InventoryCatalog.From([InventoryTestStores.NumberOne, InventoryTestStores.JadeTemu, InventoryTestStores.Medical]);

    private static SeasonTransaction Bought(string store, string item) => new() { OriginStore = store, ItemName = item };

    private static SeasonTransaction Sold(string item) => new() { ItemName = item, IsSale = true };

    private static SeasonRound Round(int index, params IEnumerable<SeasonTransaction> transactions) => new()
    {
        RoundIndex = index,
        Marketplace = new SeasonMarketplace { Transactions = transactions.ToList() }
    };

    [Fact]
    public void NoSeasonOwnsNothing()
    {
        Assert.Empty(SeasonInventory.From(null, Catalog).Entries);
    }

    [Fact]
    public void TalliesPurchasesWithTheirStoresAndRounds()
    {
        var season = new SeasonFile
        {
            InitialPurchases = new SeasonMarketplace { Transactions = [Bought("Number One [PanO, Lawful]", "Pistol")] },
            Rounds = [Round(2, Bought("Jade Temu - Round 2 Store", "Pistol"), Bought("Number One", "Combi Rifle"))]
        };

        var entries = SeasonInventory.From(season, Catalog).Entries;

        Assert.Equal(["Combi Rifle", "Pistol"], entries.Select(entry => entry.Name));
        var pistol = entries[1];
        Assert.Equal(2, pistol.Count);
        Assert.Equal(3, pistol.CostCr);
        Assert.Equal(["Jade Temu", "Number One"], pistol.Sources);
        Assert.Equal([0, 2], pistol.Rounds);
    }

    [Fact]
    public void SalesTakeOneAway()
    {
        var season = new SeasonFile { Rounds = [Round(1, Bought("Number One", "Pistol"), Bought("Number One", "Combi Rifle"), Sold("Pistol"))] };

        Assert.Equal(["Combi Rifle"], SeasonInventory.From(season, Catalog).Entries.Select(entry => entry.Name));
    }

    [Fact]
    public void ExchangesServicesAndUnknownItemsAreNotKept()
    {
        var season = new SeasonFile
        {
            Rounds = [Round(1, Bought("Number One", "SWC +1"), Bought("Medical Services", "Surgery"), Bought("Number One", "Laser Sword"))]
        };

        Assert.Empty(SeasonInventory.From(season, Catalog).Entries);
    }

    [Fact]
    public void DowntimeWinningsAreKept()
    {
        var round = new SeasonRound
        {
            RoundIndex = 3,
            Downtime = new SeasonDowntimeEntry
            {
                WonItems =
                [
                    new SeasonInventoryItem { Name = "Combi Rifle" },
                    new SeasonInventoryItem { Name = "Monofilament Blade", Category = "Secondary", Source = "Salvage" }
                ]
            }
        };

        var entries = SeasonInventory.From(new SeasonFile { Rounds = [round] }, Catalog).Entries;

        Assert.Equal(["Combi Rifle", "Monofilament Blade"], entries.Select(entry => entry.Name));
        Assert.Equal(("Primary", 12, "Downtime"), (entries[0].Category, entries[0].CostCr, entries[0].Sources.Single()));
        Assert.Equal(("Secondary", 0, "Salvage"), (entries[1].Category, entries[1].CostCr, entries[1].Sources.Single()));
        Assert.Equal(0, entries[1].SellPrice);
    }

    [Fact]
    public void ARandomPistolEffectWinsAPistol()
    {
        var round = new SeasonRound { RoundIndex = 1, Downtime = new SeasonDowntimeEntry { OtherEffects = "Gained a random pistol" } };

        var entry = Assert.Single(SeasonInventory.From(new SeasonFile { Rounds = [round] }, Catalog).Entries);

        Assert.Equal((SeasonInventory.RandomPistol, "Sidearm"), (entry.Name, entry.Category));
    }

    [Fact]
    public void ListsGearSlotsInSlotOrderThenOtherCategories()
    {
        var season = new SeasonFile
        {
            Rounds = [Round(1, Bought("Number One", "Rations"), Bought("Number One", "WIP=12"), Bought("Number One", "Light Armour (LI)"), Bought("Number One", "Pistol"))]
        };

        var entries = SeasonInventory.From(season, Catalog).Entries;

        Assert.Equal(["Sidearm", "Armor", "Augments", "General"], entries.Select(entry => entry.Category));
    }

    [Fact]
    public void ListsWhatFitsASlot()
    {
        var season = new SeasonFile { Rounds = [Round(1, Bought("Number One", "Pistol"), Bought("Jade Temu", "Pistol"), Bought("Number One", "Combi Rifle"))] };
        var inventory = SeasonInventory.From(season, Catalog);

        Assert.Equal(["Pistol"], inventory.ItemsFor(GearSlot.Sidearm));
        Assert.Empty(inventory.ItemsFor(GearSlot.Roles));
    }

    // Belts and Pouches' extra slot takes gear of any kind but armour, and nothing that isn't gear.
    [Fact]
    public void TheExtraSlotTakesAnyGearButArmour()
    {
        var season = new SeasonFile
        {
            Rounds = [Round(1, Bought("Number One", "Pistol"), Bought("Number One", "Light Armour (LI)"), Bought("Number One", "WIP=12"), Bought("Number One", "Rations"))]
        };

        Assert.Equal(["Pistol", "WIP=12"], SeasonInventory.From(season, Catalog).ItemsFor(GearSlot.Extra));
    }

    [Fact]
    public void AnExtraItemWorksAsItsOwnKind()
    {
        IReadOnlyList<SeasonUnitGear> gear =
        [
            new() { Slot = GearSlot.Armor.Key(), ItemName = "Light Armour (LI)" },
            new() { Slot = GearSlot.Extra.Key(), ItemName = "Pistol" },
            new() { Slot = GearSlot.Extra.Key(), ItemName = "Mystery Box" }
        ];

        Assert.Equal(
            [new EquippedItem(GearSlot.Armor, "Light Armour (LI)"), new EquippedItem(GearSlot.Sidearm, "Pistol")],
            EquippedItem.In(gear, Catalog));
    }

    [Theory]
    [InlineData(12, 6)]
    [InlineData(5, 3)]
    [InlineData(0, 0)]
    public void SellsForHalfRoundedUp(int cost, int sellPrice)
    {
        Assert.Equal(sellPrice, new InventoryEntry("Pistol", "Sidearm", cost, 1, [], []).SellPrice);
    }
}
