using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class UnitProfileVitalityTests
{
    [Fact]
    public void Vitality_StructureAndWounds_StructureWins() =>
        Assert.Equal(new StatValue(Stat.Str, "3"), new UnitProfile { StrValue = 3, W = 1 }.Vitality);

    [Fact]
    public void Vitality_NoStructure_FallsBackFromWoundsToVita()
    {
        Assert.Equal(new StatValue(Stat.Vita, "2"), new UnitProfile { W = 2, Vita = 1 }.Vitality);
        Assert.Equal(new StatValue(Stat.Vita, "1"), new UnitProfile { Vita = 1 }.Vitality);
        Assert.Equal(new StatValue(Stat.Vita, "-"), new UnitProfile().Vitality);
    }
}
