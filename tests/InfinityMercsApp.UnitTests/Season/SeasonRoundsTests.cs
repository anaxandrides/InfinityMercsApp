using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class SeasonRoundsTests
{
    private static SeasonRound Round(int index, int notoriety = 0) =>
        new() { RoundIndex = index, Downtime = new SeasonDowntimeEntry { NotorietyGain = notoriety } };

    [Fact]
    public void TheCurrentRoundIsTheHighestIndex()
    {
        Assert.Equal(3, SeasonRounds.Current(new SeasonFile { Rounds = [Round(1), Round(3), Round(2)] }));
    }

    [Fact]
    public void UnindexedRoundsAreCounted()
    {
        Assert.Equal(2, SeasonRounds.Current(new SeasonFile { Rounds = [Round(0), Round(0)] }));
    }

    [Fact]
    public void BeforeAnyRoundItIsRoundZero()
    {
        Assert.Equal(0, SeasonRounds.Current(new SeasonFile()));
        Assert.Equal(0, SeasonRounds.Current(null));
        Assert.Null(SeasonRounds.Latest(null));
    }

    [Fact]
    public void TheLatestRoundHasTheHighestIndex()
    {
        var latest = Round(4);

        Assert.Same(latest, SeasonRounds.Latest(new SeasonFile { Rounds = [Round(2), latest, Round(3)] }));
    }

    [Fact]
    public void NotorietyAddsUpOverTheRounds()
    {
        Assert.Equal(3, SeasonRounds.CompanyNotoriety(new SeasonFile { Rounds = [Round(1, 2), Round(2, -1), Round(3, 2)] }));
        Assert.Equal(0, SeasonRounds.CompanyNotoriety(null));
    }
}
