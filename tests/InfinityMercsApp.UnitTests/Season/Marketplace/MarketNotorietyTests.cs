using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Marketplace;

namespace InfinityMercsApp.UnitTests.Season.Marketplace;

public sealed class MarketNotorietyTests
{
    private static readonly Store Lawful = new() { Name = "Number One", Alignment = StoreAlignments.Lawful };
    private static readonly Store Chaotic = new() { Name = "Jade Temu", Alignment = StoreAlignments.Chaotic };
    private static readonly Store Neutral = new() { Name = "Haven", Alignment = StoreAlignments.Neutral };

    [Fact]
    public void For_LawfulMarket_ChargesMoreForNotoriety()
    {
        var notoriety = MarketNotoriety.For(Lawful, 3);

        Assert.Equal(13, notoriety.Price(10));
        Assert.Equal("Lawful  (Notoriety: +3 CR)", notoriety.AlignmentText(Lawful.Alignment));
    }

    [Fact]
    public void For_ChaoticMarket_ChargesLessButNeverBelowHalf()
    {
        var notoriety = MarketNotoriety.For(Chaotic, 4);

        Assert.Equal(6, notoriety.Price(10));
        Assert.Equal(3, notoriety.Price(5));
        Assert.Equal("Chaotic  (Notoriety: -4 CR)", notoriety.AlignmentText(Chaotic.Alignment));
    }

    [Fact]
    public void For_NeutralMarketOrNoNotoriety_ChangesNothing()
    {
        Assert.Equal(10, MarketNotoriety.For(Neutral, 5).Price(10));
        Assert.Equal("Neutral", MarketNotoriety.For(Neutral, 5).AlignmentText("Neutral"));
        Assert.Equal("Lawful", MarketNotoriety.For(Lawful, 0).AlignmentText("Lawful"));
    }
}
