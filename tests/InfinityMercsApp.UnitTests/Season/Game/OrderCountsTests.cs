using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.UnitTests.Season.Game;

public sealed class OrderCountsTests
{
    [Fact]
    public void ConsciousTroopersGiveOrdersPlusTheMercsBonus()
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
    public void AnEmptyCompanyStillGetsTheBonusOrder() => Assert.Equal(new OrderCounts(0, 0, 0, 1), OrderCounts.For([]));
}
