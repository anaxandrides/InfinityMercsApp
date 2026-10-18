using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class StatKeysTests
{
    [Fact]
    public void TryParse_EveryStatsKey_RoundTrips()
    {
        foreach (var stat in Enum.GetValues<Stat>())
        {
            Assert.True(StatKeys.TryParse(stat.Key(), out var parsed));
            Assert.Equal(stat, parsed);
        }
    }

    [Theory]
    [InlineData("cc", Stat.Cc)]
    [InlineData(" Wip ", Stat.Wip)]
    [InlineData("str", Stat.Str)]
    public void TryParse_CaseAndSpaces_IgnoresThem(string key, Stat expected)
    {
        Assert.True(StatKeys.TryParse(key, out var stat));
        Assert.Equal(expected, stat);
    }

    [Theory]
    [InlineData("XP")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_OtherKey_ReturnsFalse(string? key) => Assert.False(StatKeys.TryParse(key, out _));

    [Fact]
    public void Key_Stat_IsTheStatlineSpelling()
    {
        Assert.Equal("MOV", Stat.Mov.Key());
        Assert.Equal("VITA", Stat.Vita.Key());
        Assert.Equal("AVA", Stat.Ava.Key());
    }

    [Theory]
    [InlineData("TAG", Stat.Str)]
    [InlineData(" rem ", Stat.Str)]
    [InlineData("PERIPHERAL", Stat.Str)]
    [InlineData("LI", Stat.Vita)]
    [InlineData(null, Stat.Vita)]
    public void VitalityFor_StructureUnit_UsesStr(string? unitTypeCode, Stat expected) =>
        Assert.Equal(expected, StatKeys.VitalityFor(unitTypeCode));

    [Fact]
    public void IsVitality_Stats_TrueForVitaStrAndW() =>
        Assert.Equal<Stat>([Stat.Vita, Stat.Str, Stat.W], Enum.GetValues<Stat>().Where(StatKeys.IsVitality));

    [Fact]
    public void IsTag_AnyCase_RecognisesTag()
    {
        Assert.True(UnitTypeCodes.IsTag(" tag "));
        Assert.False(UnitTypeCodes.IsTag("REM"));
    }
}
