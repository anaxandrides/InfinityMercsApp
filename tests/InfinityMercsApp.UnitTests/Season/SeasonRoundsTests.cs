using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class SeasonRoundsTests
{
    private static SeasonRound Round(int index, int notoriety = 0) =>
        new() { RoundIndex = index, Downtime = new SeasonDowntimeEntry { NotorietyGain = notoriety } };

    [Fact]
    public void Current_SeveralRounds_ReturnsTheHighestIndex()
    {
        Assert.Equal(3, SeasonRounds.Current(new SeasonFile { Rounds = [Round(1), Round(3), Round(2)] }));
    }

    [Fact]
    public void Current_UnindexedRounds_CountsThem()
    {
        Assert.Equal(2, SeasonRounds.Current(new SeasonFile { Rounds = [Round(0), Round(0)] }));
    }

    [Fact]
    public void CurrentAndLatest_NoRounds_AreRoundZero()
    {
        Assert.Equal(0, SeasonRounds.Current(new SeasonFile()));
        Assert.Equal(0, SeasonRounds.Current(null));
        Assert.Null(SeasonRounds.Latest(null));
    }

    [Fact]
    public void Latest_SeveralRounds_ReturnsTheHighestIndex()
    {
        var latest = Round(4);

        Assert.Same(latest, SeasonRounds.Latest(new SeasonFile { Rounds = [Round(2), latest, Round(3)] }));
    }

    [Fact]
    public void CompanyNotoriety_SeveralRounds_AddsThemUp()
    {
        Assert.Equal(3, SeasonRounds.CompanyNotoriety(new SeasonFile { Rounds = [Round(1, 2), Round(2, -1), Round(3, 2)] }));
        Assert.Equal(0, SeasonRounds.CompanyNotoriety(null));
    }

    [Fact]
    public void CurrentMarketplace_NoRounds_ReturnsTheInitialPurchases()
    {
        var season = new SeasonFile();

        Assert.Same(season.InitialPurchases, SeasonRounds.CurrentMarketplace(season));
    }

    [Fact]
    public void CurrentMarketplace_AfterAContract_ReturnsTheLatestRounds()
    {
        var season = new SeasonFile { Rounds = [Round(2), Round(1)] };

        Assert.Same(season.Rounds[0].Marketplace, SeasonRounds.CurrentMarketplace(season));
    }
}
