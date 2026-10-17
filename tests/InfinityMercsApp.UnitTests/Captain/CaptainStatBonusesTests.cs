using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class CaptainStatBonusesTests
{
    private static readonly CaptainStatBonuses Bonuses = new(Cc: 2, Bs: 1, Ph: 0, Wip: 3, Arm: 1, Bts: 6, Vitality: 1);

    [Theory]
    [InlineData(Stat.Vita, 1)]
    [InlineData(Stat.Str, 1)]
    [InlineData(Stat.W, 1)]
    [InlineData(Stat.Bts, 6)]
    [InlineData(Stat.Mov, 0)]
    public void For_SharesTheVitalityBonusAcrossVitaStrAndW(Stat stat, int expected) => Assert.Equal(expected, Bonuses.For(stat));

    [Theory]
    [InlineData("13", 2, "15")]
    [InlineData("-", 2, "-")]
    [InlineData("T", 1, "T")]
    [InlineData("13", 0, "13")]
    public void Apply_AddsToWholeNumbersOnly(string value, int bonus, string expected) =>
        Assert.Equal(expected, CaptainStatBonuses.Apply(value, bonus));

    [Fact]
    public void ApplyToStatline_AddsEachBonusAndKeepsOtherSegments() =>
        Assert.Equal(
            "MOV 10-5 | CC 16 | BS 13 | PH 10 | WIP 16 | ARM 2 | BTS 9 | VITA 2 | S 2",
            Bonuses.ApplyToStatline("MOV 10-5 | CC 14 | BS 12 | PH 10 | WIP 13 | ARM 1 | BTS 3 | VITA 1 | S 2"));

    [Fact]
    public void ApplyToStatline_LeavesTextWithoutStatsUnchanged()
    {
        Assert.Equal("Captain", Bonuses.ApplyToStatline("Captain"));
        Assert.Equal(string.Empty, Bonuses.ApplyToStatline(null));
    }

    [Fact]
    public void BoughtBonusesReadAsOnAnArmySheet()
    {
        var bonuses = new CaptainStatBonuses(Cc: 2, Bs: 0, Ph: 0, Wip: 1, Arm: 0, Bts: 0, Vitality: 1);

        Assert.Equal(["+2 CC", "+1 WIP", "+1 VITA"], bonuses.Lines(Stat.Vita));
        Assert.Equal("+1 STR", bonuses.Lines(Stat.Str)[^1]);
        Assert.Empty(CaptainStatBonuses.None.Lines(Stat.Vita));
    }
}
