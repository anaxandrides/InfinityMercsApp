using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Troopers;

public sealed class PerkSlotsTests
{
    [Theory]
    [InlineData("Hacker (Role) No device", GearSlot.Roles)]
    [InlineData("Doctor (ReRoll -3) (Role)", GearSlot.Roles)]
    [InlineData("360º Visor", GearSlot.Accessories)]
    [InlineData("Holomask", GearSlot.Accessories)]
    [InlineData("Medikit (no slot required)", null)]
    [InlineData("Hacking Device", null)]
    [InlineData("+1 BS", null)]
    public void SlotOf_Perk_IsTheSlotItFills(string perkName, GearSlot? expected) => Assert.Equal(expected, PerkSlots.SlotOf(perkName));

    [Fact]
    public void Filled_Perks_NamesThePerkInEachSlot()
    {
        var filled = PerkSlots.Filled(["intelligence-track-4-tier-1", "empathy-track-7-tier-3", "body-track-1-tier-1"]);

        Assert.Equal("Hacker (Role) No device", filled[GearSlot.Roles]);
        Assert.Equal("Holomask", filled[GearSlot.Accessories]);
        Assert.Equal(2, filled.Count);
    }
}
