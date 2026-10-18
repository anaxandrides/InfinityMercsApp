using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.UnitTests.Season.Inventory;

public sealed class InventoryCatalogTests
{
    private static readonly InventoryCatalog Catalog =
        InventoryCatalog.From([InventoryTestStores.NumberOne, InventoryTestStores.JadeTemu, InventoryTestStores.Medical]);

    [Fact]
    public void Find_StoreAndName_FindsTheItem()
    {
        Assert.Equal(new CatalogItem("Pistol", "Sidearm", "Jade Temu", 5), Catalog.Find("Jade Temu", "Pistol"));
    }

    [Fact]
    public void Find_Item_KnowsItsEquippedSwc()
    {
        Assert.Equal(1.5m, Catalog.Find("Spitfire")?.CostSwc);
        Assert.Equal(0m, Catalog.Find("Light Armour (LI)")?.CostSwc);
    }

    [Fact]
    public void Find_StoreLabel_ReadsTheStore()
    {
        Assert.Equal(5, Catalog.Find("Jade Temu - Round 2 Store", "Pistol")?.CostCr);
    }

    [Fact]
    public void Find_UnknownStore_FallsBackToAnyStoreSellingIt()
    {
        Assert.Equal("Number One", Catalog.Find("Somewhere Else", "Combi Rifle")?.StoreName);
    }

    [Fact]
    public void Find_TroopTypeOrAugment_ListsAsArmorOrAugments()
    {
        Assert.Equal("Armor", Catalog.Find("Number One", "Light Armour (LI)")?.Category);
        Assert.Equal("Augments", Catalog.Find("WIP=12")?.Category);
    }

    [Fact]
    public void Find_UncategorisedItem_IsGeneral()
    {
        Assert.Equal("General", Catalog.Find("Rations")?.Category);
    }

    [Fact]
    public void Find_Service_ReturnsNull()
    {
        Assert.Null(Catalog.Find("Surgery"));
    }
}
