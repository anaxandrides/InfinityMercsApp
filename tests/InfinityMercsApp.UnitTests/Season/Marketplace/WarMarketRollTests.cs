using InfinityMercsApp.Domain.Season.Marketplace;

namespace InfinityMercsApp.UnitTests.Season.Marketplace;

public sealed class WarMarketRollTests
{
    private static readonly IReadOnlySet<string> NumberOneIsOurs = new HashSet<string>(["Number One"], StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void For_Roll_NamesItsMarket()
    {
        Assert.Equal(new WarMarketRoll(3, WarMarketRollKind.Found, "Jade Temu"), WarMarketRoll.For(3, NumberOneIsOurs));
    }

    [Fact]
    public void For_CompanysOwnMarket_IsAFactionMarket()
    {
        Assert.Equal(new WarMarketRoll(1, WarMarketRollKind.FactionMarket, "Number One"), WarMarketRoll.For(1, NumberOneIsOurs));
    }

    [Theory]
    [InlineData(19)]
    [InlineData(20)]
    public void For_19Or20_ChoosesAnyMarket(int roll)
    {
        Assert.Equal(WarMarketRollKind.ChooseAny, WarMarketRoll.For(roll, NumberOneIsOurs).Kind);
    }

    [Fact]
    public void For_OffTableRoll_IsInvalid()
    {
        Assert.Equal(WarMarketRollKind.Invalid, WarMarketRoll.For(0, NumberOneIsOurs).Kind);
    }

    [Fact]
    public void Search_CompanysOwnMarket_Rerolls()
    {
        var rolls = new Queue<int>([1, 2, 5]);

        var result = WarMarketRoll.Search(rolls.Dequeue, NumberOneIsOurs);

        Assert.Equal(new WarMarketRoll(5, WarMarketRollKind.Found, "Arachne Req"), result);
    }

    [Fact]
    public void Search_FreeChoiceRolled_Stops()
    {
        var rolls = new Queue<int>([1, 20]);

        Assert.Equal(WarMarketRollKind.ChooseAny, WarMarketRoll.Search(rolls.Dequeue, NumberOneIsOurs).Kind);
    }

    [Fact]
    public void Search_TooManyRerolls_GivesUp()
    {
        var attempts = 0;

        var result = WarMarketRoll.Search(() => { attempts++; return 1; }, NumberOneIsOurs);

        Assert.Equal(WarMarketRollKind.Exhausted, result.Kind);
        Assert.Equal(WarMarketRoll.MaxAttempts, attempts);
    }
}
