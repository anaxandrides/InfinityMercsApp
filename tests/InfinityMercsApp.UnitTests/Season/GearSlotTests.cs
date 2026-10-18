using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class GearSlotTests
{
    [Fact]
    public void HoldsWeapon_Slot_TrueForWeaponSlots()
    {
        Assert.True(GearSlot.Sidearm.HoldsWeapon());
        Assert.False(GearSlot.Augments.HoldsWeapon());
    }

    [Fact]
    public void TryParse_SlotName_ReadsIgnoringCase()
    {
        Assert.True(GearSlots.TryParse(" sidearm ", out var slot));
        Assert.Equal(GearSlot.Sidearm, slot);
        Assert.False(GearSlots.TryParse("Helmet", out _));
    }

    [Fact]
    public void ToSeasonGear_Loadout_KeepsOneItemPerSlotInSlotOrder()
    {
        var loadout = new UnitLoadout { [GearSlot.Augments] = "WIP=12", [GearSlot.Primary] = "Spitfire" };
        loadout[GearSlot.Primary] = "Combi Rifle";
        loadout[GearSlot.Roles] = "Medic";
        loadout[GearSlot.Roles] = " ";

        var saved = loadout.ToSeasonGear();

        Assert.Equal(["Primary:Combi Rifle", "Augments:WIP=12"], saved.Select(g => $"{g.Slot}:{g.ItemName}"));
    }

    [Fact]
    public void From_UnknownSlot_IgnoresIt()
    {
        var loadout = UnitLoadout.From([new SeasonUnitGear { Slot = "Armor", ItemName = "Kevlar" }, new SeasonUnitGear { Slot = "Hat", ItemName = "Beret" }]);

        Assert.Equal("Kevlar", loadout[GearSlot.Armor]);
        Assert.Single(loadout.ToSeasonGear());
    }

    // A pistol in one trooper's extra slot is the same pistol another would carry as a sidearm.
    [Fact]
    public void Assign_BetweenOwnSlotAndExtra_MovesTheCopy()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Sidearm, "Pistol");

        loadouts.Assign(2, GearSlot.Extra, "Pistol", takenFrom: 1);
        Assert.Null(loadouts.For(1)[GearSlot.Sidearm]);

        loadouts.Assign(2, GearSlot.Sidearm, "Pistol", takenFrom: 2);
        Assert.Equal(("Pistol", null), (loadouts.For(2)[GearSlot.Sidearm], loadouts.For(2)[GearSlot.Extra]));
    }

    [Fact]
    public void SharesItemsWith_Slots_OnlyExtraSharesWithOthers()
    {
        Assert.True(GearSlot.Sidearm.SharesItemsWith(GearSlot.Extra));
        Assert.True(GearSlot.Sidearm.SharesItemsWith(GearSlot.Sidearm));
        Assert.False(GearSlot.Sidearm.SharesItemsWith(GearSlot.Primary));
    }

    [Theory]
    [InlineData("Primary", 0)]
    [InlineData("Augments", 6)]
    [InlineData("Exchange", 7)]
    [InlineData("Tools", 50)]
    [InlineData("General", 99)]
    [InlineData("primary", 50)]
    public void Order_Category_ListsSlotsFirstAndGeneralLast(string category, int order) =>
        Assert.Equal(order, InventoryCategories.Order(category));

    [Fact]
    public void Normalize_BlankCategory_ReturnsGeneral() => Assert.Equal("General", InventoryCategories.Normalize("  "));
}
