using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.UnitTests.Season.Game;

public sealed class OrderCountsTests
{
    [Fact]
    public void For_ConsciousTroopers_GiveOrdersPlusTheMercsBonus()
    {
        var counts = OrderCounts.For(
        [
            new OrderSource(IsLieutenant: true, IsImpetuous: false, IsIrregular: false, IsConscious: true),
            new OrderSource(false, true, true, true),
            new OrderSource(false, false, false, true),
            new OrderSource(false, false, false, false)
        ]);

        Assert.Equal(new OrderCounts(1, 1, 1, 3), counts);
        Assert.Equal(3, counts.Of(OrderKind.Regular));
        Assert.Equal(1, counts.Of(OrderKind.Lieutenant));
    }

    [Fact]
    public void For_HireOverTheTrooperLimit_GivesOnlyItsImpetuousOrder()
    {
        var counts = OrderCounts.For(
        [
            new OrderSource(IsLieutenant: false, IsImpetuous: true, IsIrregular: true, IsConscious: true, IsOverTrooperLimit: true),
            new OrderSource(false, false, false, true, IsOverTrooperLimit: true)
        ]);

        Assert.Equal(new OrderCounts(0, 1, 0, 1), counts);
    }

    [Fact]
    public void For_NoTroopers_StillGivesTheBonusOrder() => Assert.Equal(new OrderCounts(0, 0, 0, 1), OrderCounts.For([]));
}
