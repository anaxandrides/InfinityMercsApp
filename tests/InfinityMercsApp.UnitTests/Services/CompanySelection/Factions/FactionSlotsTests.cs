using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.StartCompany;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Factions;

public sealed class FactionSlotsTests
{
    [Theory]
    [InlineData(0, 101, 102, 102, true)]
    [InlineData(0, 101, 102, 101, false)]
    [InlineData(1, 101, 102, 101, true)]
    [InlineData(1, null, 102, 101, false)]
    [InlineData(0, 102, 102, 102, false)]
    public void IsInOtherSlot_FactionPicked_TrueWhenTheOtherSlotHasIt(int slot, int? left, int? right, int picked, bool inOtherSlot) =>
        Assert.Equal(inOtherSlot, FactionSlots.IsInOtherSlot(hasRightSlot: true, slot, left, right, picked));

    [Fact]
    public void IsInOtherSlot_NoRightSlot_IsFalse() =>
        Assert.False(FactionSlots.IsInOtherSlot(hasRightSlot: false, 0, 101, 102, 102));

    [Theory]
    [InlineData(1, true, 1)]
    [InlineData(1, false, 0)]
    [InlineData(0, true, 0)]
    public void Active_SlotTapped_IsThatSlotWhenItExists(int tapped, bool hasRightSlot, int active) =>
        Assert.Equal(active, FactionSlots.Active(tapped, hasRightSlot));

    [Theory]
    [InlineData(true, false, 0, 1)]
    [InlineData(false, true, 1, 0)]
    [InlineData(true, true, 1, 1)]
    [InlineData(false, false, 1, 1)]
    public void AfterFilling_OneSlotEmpty_ReturnsTheEmptyOne(bool leftFilled, bool rightFilled, int current, int next) =>
        Assert.Equal(next, FactionSlots.AfterFilling(hasRightSlot: true, leftFilled, rightFilled, current));

    [Fact]
    public void SourceFactions_BothSlotsDiffer_ListsBoth() =>
        Assert.Equal([Left, Right], FactionSlots.SourceFactions(hasRightSlot: true, Left, Right));

    [Fact]
    public void SourceFactions_SameFactionInBothSlots_ListsItOnce() =>
        Assert.Equal([Left], FactionSlots.SourceFactions(hasRightSlot: true, Left, Left with { Name = "Again" }));

    [Fact]
    public void SourceFactions_NoRightSlot_ListsOnlyTheLeft() =>
        Assert.Equal([Left], FactionSlots.SourceFactions(hasRightSlot: false, Left, Right));

    [Theory]
    [InlineData(0, 101)]
    [InlineData(1, 102)]
    public void ActiveSlotFaction_Slot_ListsOnlyThatSlotsFaction(int slot, int expected) =>
        Assert.Equal([expected], FactionSlots.ActiveSlotFaction(slot, Left, Right).Select(faction => faction.Id));

    private static readonly SlotFaction Left = new(101, 101, "PanOceania");
    private static readonly SlotFaction Right = new(102, 101, "Military Orders");

    private sealed record SlotFaction(int Id, int ParentId, string Name) : ICompanySourceFaction;
}
