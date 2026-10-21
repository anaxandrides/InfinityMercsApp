using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Presentation.Captain;

namespace InfinityMercsApp.UnitTests.Presentation.Captain;

public sealed class CaptainStatCellTests
{
    [Fact]
    public void For_NumericStatWithBonus_AddsTheBonus() =>
        Assert.Equal(
            new CaptainStatCell("WIP", "15", true),
            CaptainStatCell.For(new StatValue(Stat.Wip, "12"), CaptainStatBonuses.None with { Wip = 3 }));

    [Fact]
    public void For_StatWithoutBonus_IsUnmodified() =>
        Assert.Equal(
            new CaptainStatCell("BS", "12", false),
            CaptainStatCell.For(new StatValue(Stat.Bs, "12"), CaptainStatBonuses.None with { Cc = 5 }));

    [Fact]
    public void For_NonNumericValue_ShowsItAsItIs() =>
        Assert.Equal(
            new CaptainStatCell("MOV", "10-5", false),
            CaptainStatCell.For(new StatValue(Stat.Mov, "10-5"), CaptainStatBonuses.None with { Vitality = 1 }));
}
