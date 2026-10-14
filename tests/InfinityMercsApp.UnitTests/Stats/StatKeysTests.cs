using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class StatKeysTests
{
    [Fact]
    public void EveryStatRoundTripsThroughItsKey()
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
    public void ParsingIgnoresCaseAndSpaces(string key, Stat expected)
    {
        Assert.True(StatKeys.TryParse(key, out var stat));
        Assert.Equal(expected, stat);
    }

    [Theory]
    [InlineData("XP")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherKeysAreNotStats(string? key) => Assert.False(StatKeys.TryParse(key, out _));

    [Fact]
    public void KeysAreTheStatlineSpelling()
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
    public void StructureUnitsUseStrInsteadOfVita(string? unitTypeCode, Stat expected) =>
        Assert.Equal(expected, StatKeys.VitalityFor(unitTypeCode));

    [Fact]
    public void VitaStrAndWAreTheVitalityStats() =>
        Assert.Equal<Stat>([Stat.Vita, Stat.Str, Stat.W], Enum.GetValues<Stat>().Where(StatKeys.IsVitality));

    [Fact]
    public void TagIsRecognisedWhateverItsCase()
    {
        Assert.True(UnitTypeCodes.IsTag(" tag "));
        Assert.False(UnitTypeCodes.IsTag("REM"));
    }
}
