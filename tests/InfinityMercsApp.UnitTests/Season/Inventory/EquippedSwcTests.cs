using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.UnitTests.Season.Inventory;

public sealed class EquippedSwcTests
{
    private static readonly InventoryCatalog Catalog = InventoryCatalog.From([InventoryTestStores.NumberOne]);

    [Fact]
    public void Of_Item_ReturnsItsCatalogSwc()
    {
        Assert.Equal(1.5, EquippedSwc.Of("Spitfire", Catalog));
        Assert.Equal(0, EquippedSwc.Of("Pistol", Catalog));
        Assert.Equal(0, EquippedSwc.Of("Unknown Gun", Catalog));
    }

    [Fact]
    public void Of_Loadouts_AddsEveryTroopersGear()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Primary, "Spitfire");
        loadouts.Assign(1, GearSlot.Sidearm, "Pistol");
        loadouts.Assign(2, GearSlot.Secondary, "Spitfire");

        Assert.Equal(3, EquippedSwc.Of(loadouts, Catalog));
    }

    [Fact]
    public void After_Equipping_AddsTheItemLessWhatWasInTheSlot()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Secondary, "Spitfire");
        loadouts.Assign(2, GearSlot.Primary, "Spitfire");

        Assert.Equal(1.5, EquippedSwc.After(loadouts, 2, GearSlot.Primary, "Pistol", Catalog));
        Assert.Equal(1.5, EquippedSwc.After(loadouts, 2, GearSlot.Primary, null, Catalog));
        Assert.Equal(4.5, EquippedSwc.After(loadouts, 3, GearSlot.Sidearm, "Spitfire", Catalog));
    }

    [Fact]
    public void After_TakenFromAnotherTrooper_AddsNothing()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Primary, "Spitfire");

        Assert.Equal(1.5, EquippedSwc.After(loadouts, 2, GearSlot.Primary, "Spitfire", Catalog, takenFrom: 1));
    }

    [Fact]
    public void After_SpareCopy_AddsItsSwc()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Primary, "Spitfire");

        Assert.Equal(3, EquippedSwc.After(loadouts, 2, GearSlot.Primary, "Spitfire", Catalog));
    }
}
