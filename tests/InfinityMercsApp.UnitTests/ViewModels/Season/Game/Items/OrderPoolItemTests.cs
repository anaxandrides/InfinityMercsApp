using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.ViewModels.Season.Game.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Game.Items;

public sealed class OrderPoolItemTests
{
    [Fact]
    public void OrdersMoveBetweenAvailableAndUsed()
    {
        var pool = new OrderPoolItem(OrderKind.Regular);
        pool.Reset(2);

        pool.UseCommand.Execute(null);
        Assert.Equal((1, 1), (pool.Available, pool.Used));

        pool.RestoreCommand.Execute(null);
        Assert.Equal((2, 0), (pool.Available, pool.Used));
    }

    [Fact]
    public void NothingMovesFromAnEmptySide()
    {
        var pool = new OrderPoolItem(OrderKind.Lieutenant);
        pool.Reset(0);

        Assert.False(pool.UseCommand.CanExecute(null));
        Assert.False(pool.RestoreCommand.CanExecute(null));
    }

    [Fact]
    public void ResettingClearsSpentOrders()
    {
        var pool = new OrderPoolItem(OrderKind.Irregular);
        pool.Reset(1);
        pool.UseCommand.Execute(null);

        pool.Reset(3);

        Assert.Equal((3, 0), (pool.Available, pool.Used));
        Assert.True(pool.UseCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(OrderKind.Lieutenant, "lieutenant")]
    [InlineData(OrderKind.Impetuous, "impetuous")]
    [InlineData(OrderKind.Irregular, "irregular")]
    [InlineData(OrderKind.Regular, "regular")]
    public void EachKindHasItsIcon(OrderKind kind, string icon) =>
        Assert.Equal($"SVGCache/CBIcons/{icon}.svg", new OrderPoolItem(kind).IconPath);
}
