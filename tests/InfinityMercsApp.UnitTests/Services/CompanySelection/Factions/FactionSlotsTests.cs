using InfinityMercsApp.Services.CompanySelection.Factions;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Factions;

public sealed class FactionSlotsTests
{
    [Theory]
    [InlineData(0, 101, 102, 102, true)]
    [InlineData(0, 101, 102, 101, false)]
    [InlineData(1, 101, 102, 101, true)]
    [InlineData(1, null, 102, 101, false)]
    [InlineData(0, 102, 102, 102, false)]
    public void AFactionCannotTakeBothSlots(int slot, int? left, int? right, int picked, bool inOtherSlot) =>
        Assert.Equal(inOtherSlot, FactionSlots.IsInOtherSlot(hasRightSlot: true, slot, left, right, picked));

    [Fact]
    public void WithoutARightSlotNothingIsInTheOtherSlot() =>
        Assert.False(FactionSlots.IsInOtherSlot(hasRightSlot: false, 0, 101, 102, 102));

    [Theory]
    [InlineData(1, true, 1)]
    [InlineData(1, false, 0)]
    [InlineData(0, true, 0)]
    public void TheActiveSlotIsTheOneTappedWhenItExists(int tapped, bool hasRightSlot, int active) =>
        Assert.Equal(active, FactionSlots.Active(tapped, hasRightSlot));

    [Theory]
    [InlineData(true, false, 0, 1)]
    [InlineData(false, true, 1, 0)]
    [InlineData(true, true, 1, 1)]
    [InlineData(false, false, 1, 1)]
    public void AfterFillingASlotTheEmptyOneIsNext(bool leftFilled, bool rightFilled, int current, int next) =>
        Assert.Equal(next, FactionSlots.AfterFilling(hasRightSlot: true, leftFilled, rightFilled, current));
}
