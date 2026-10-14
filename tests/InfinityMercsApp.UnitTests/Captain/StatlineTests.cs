using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class StatlineTests
{
    [Fact]
    public void NumericStats_ReadTheCombatStats() =>
        Assert.Equal(
            new Dictionary<Stat, int> { [Stat.Cc] = 14, [Stat.Bs] = 12, [Stat.Vita] = 1 },
            Statline.NumericStats("MOV 10-5 | cc 14 | BS 12 | VITA 1 | AVA T"));

    [Fact]
    public void Entries_KeepEveryStatSegmentInOrder() =>
        Assert.Equal<StatValue>(
            [new(Stat.Mov, "10-5"), new(Stat.Cc, "14"), new(Stat.Ava, "T")],
            Statline.Entries("MOV 10-5 | cc 14 | junk | XP 3 | AVA T"));
}
