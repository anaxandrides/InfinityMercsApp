using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class StatlineTests
{
    [Fact]
    public void NumericStats_Statline_ReadsTheCombatStats() =>
        Assert.Equal(
            new Dictionary<Stat, int> { [Stat.Cc] = 14, [Stat.Bs] = 12, [Stat.Vita] = 1 },
            Statline.NumericStats("MOV 10-5 | cc 14 | BS 12 | VITA 1 | AVA T"));

    [Fact]
    public void Entries_StatlineWithJunk_KeepsEveryStatSegmentInOrder() =>
        Assert.Equal<StatValue>(
            [new(Stat.Mov, "10-5"), new(Stat.Cc, "14"), new(Stat.Ava, "T")],
            Statline.Entries("MOV 10-5 | cc 14 | junk | XP 3 | AVA T"));

    [Fact]
    public void WithMove_Statline_ReplacesOnlyTheMove() =>
        Assert.Equal("MOV 4-2 | CC 14 | BS 12", Statline.WithMove("MOV 10-5 | CC 14 | BS 12", "4-2"));

    [Fact]
    public void WithMove_NoStatline_ReturnsJustTheMove() =>
        Assert.Equal("MOV 4-2", Statline.WithMove(null, "4-2"));
}
