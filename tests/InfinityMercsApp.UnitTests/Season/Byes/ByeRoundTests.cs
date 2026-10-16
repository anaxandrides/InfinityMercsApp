using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Byes;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Byes;

public sealed class ByeRoundTests
{
    private static IReadOnlyList<ByeTrooper> Troopers(int count) =>
        Enumerable.Range(1, count).Select(index => new ByeTrooper(index, $"Trooper {index}")).ToList();

    [Theory]
    [InlineData(ByeXpSplit.FourTroopers, 4, 7)]
    [InlineData(ByeXpSplit.SixTroopers, 6, 5)]
    public void EachSplitHasItsLimitAndXp(ByeXpSplit split, int mostTroopers, int xpEach)
    {
        Assert.Equal(mostTroopers, ByeRound.MostTroopers(split));
        Assert.Equal(xpEach, ByeRound.XpEach(split));
    }

    [Theory]
    [InlineData(ByeXpSplit.FourTroopers, 0, true)]
    [InlineData(ByeXpSplit.FourTroopers, 4, true)]
    [InlineData(ByeXpSplit.FourTroopers, 5, false)]
    [InlineData(ByeXpSplit.SixTroopers, 6, true)]
    [InlineData(ByeXpSplit.SixTroopers, 7, false)]
    public void NoMoreTroopersThanTheSplitAllows(ByeXpSplit split, int count, bool valid) =>
        Assert.Equal(valid, ByeRound.IsValid(new ByeRoundChoice(split, Troopers(count))));

    [Fact]
    public void ATrooperGainsTheXpOnlyOnce() =>
        Assert.False(ByeRound.IsValid(new ByeRoundChoice(ByeXpSplit.FourTroopers, [new(1, "A"), new(1, "A")])));

    [Fact]
    public void TheByeIsTheNextRoundWithTheChosenTroopersXp()
    {
        var season = new SeasonFile { Rounds = [new SeasonRound { RoundIndex = 1 }, new SeasonRound { RoundIndex = 2 }] };

        var round = ByeRound.Apply(season, new ByeRoundChoice(ByeXpSplit.SixTroopers, [new(3, "Bolt"), new(5, "Kestrel")]));

        Assert.Same(round, season.Rounds[^1]);
        Assert.Equal(3, round.RoundIndex);
        Assert.True(round.MissionResults.IsBye);
        Assert.False(round.MissionResults.Won);
        Assert.Equal([(3, "Bolt", 5), (5, "Kestrel", 5)],
            round.MissionResults.UnitResults.Select(result => (result.EntryIndex ?? 0, result.UnitName, result.XpGained)));
        Assert.Equal(5, SeasonExperience.GainedBy(season, 3, "Bolt"));
    }

    [Fact]
    public void TheChosenInjuryIsWrittenOff()
    {
        var season = new SeasonFile();
        season.TrooperFor(2).Injuries.AddRange([Injury.Arms, Injury.EyesInjury]);
        season.TrooperFor(4).Injuries.Add(Injury.Arms);

        ByeRound.Apply(season, new ByeRoundChoice(ByeXpSplit.FourTroopers, [], new InjuryWriteOff(2, Injury.Arms)));

        Assert.Equal([Injury.EyesInjury], season.Troopers[2].Injuries);
        Assert.Equal([Injury.Arms], season.Troopers[4].Injuries);
        Assert.Equal(1, season.Rounds[0].RoundIndex);
    }

    [Fact]
    public void AnInvalidChoiceIsRefused()
    {
        var season = new SeasonFile();

        Assert.Throws<ArgumentException>(() => ByeRound.Apply(season, new ByeRoundChoice(ByeXpSplit.FourTroopers, Troopers(5))));
        Assert.Empty(season.Rounds);
    }
}
