using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class GearSlotTests
{
    [Fact]
    public void WeaponSlotsHoldWeapons()
    {
        Assert.True(GearSlot.Sidearm.HoldsWeapon());
        Assert.False(GearSlot.Augments.HoldsWeapon());
    }

    [Fact]
    public void SlotNamesReadIgnoringCase()
    {
        Assert.True(GearSlots.TryParse(" sidearm ", out var slot));
        Assert.Equal(GearSlot.Sidearm, slot);
        Assert.False(GearSlots.TryParse("Helmet", out _));
    }

    [Fact]
    public void ALoadoutKeepsOneItemPerSlotAndSavesInSlotOrder()
    {
        var loadout = new UnitLoadout { [GearSlot.Augments] = "WIP=12", [GearSlot.Primary] = "Spitfire" };
        loadout[GearSlot.Primary] = "Combi Rifle";
        loadout[GearSlot.Roles] = "Medic";
        loadout[GearSlot.Roles] = " ";

        var saved = loadout.ToSeasonGear();

        Assert.Equal(["Primary:Combi Rifle", "Augments:WIP=12"], saved.Select(g => $"{g.Slot}:{g.ItemName}"));
    }

    [Fact]
    public void LoadingIgnoresUnknownSlots()
    {
        var loadout = UnitLoadout.From([new SeasonUnitGear { Slot = "Armor", ItemName = "Kevlar" }, new SeasonUnitGear { Slot = "Hat", ItemName = "Beret" }]);

        Assert.Equal("Kevlar", loadout[GearSlot.Armor]);
        Assert.Single(loadout.ToSeasonGear());
    }

    [Theory]
    [InlineData("Primary", 0)]
    [InlineData("Augments", 6)]
    [InlineData("Exchange", 7)]
    [InlineData("Tools", 50)]
    [InlineData("General", 99)]
    [InlineData("primary", 50)]
    public void InventoryCategoriesListSlotsFirstAndGeneralLast(string category, int order) =>
        Assert.Equal(order, InventoryCategories.Order(category));

    [Fact]
    public void BlankCategoriesAreGeneral() => Assert.Equal("General", InventoryCategories.Normalize("  "));
}
