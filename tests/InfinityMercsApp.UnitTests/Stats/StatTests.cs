using InfinityMercsApp.Domain.Models.Army.Profiles;

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

public sealed class StatOverrideTests
{
    [Theory]
    [InlineData("WIP=12", Stat.Wip, 12)]
    [InlineData(" ph = 14 ", Stat.Ph, 14)]
    [InlineData("STR=2", Stat.Str, 2)]
    public void ReadsKeyEqualsValue(string text, Stat stat, int value)
    {
        Assert.True(StatOverride.TryParse(text, out var statOverride));
        Assert.Equal(new StatOverride(stat, value), statOverride);
    }

    [Theory]
    [InlineData("Mimetism")]
    [InlineData("XP=3")]
    [InlineData("WIP=high")]
    [InlineData(null)]
    public void AnythingElseIsNotAnOverride(string? text) => Assert.False(StatOverride.TryParse(text, out _));
}

public sealed class UnitProfileVitalityTests
{
    [Fact]
    public void StructureWinsOverWounds() =>
        Assert.Equal(new StatValue(Stat.Str, "3"), new UnitProfile { StrValue = 3, W = 1 }.Vitality);

    [Fact]
    public void WoundsFallBackToVita()
    {
        Assert.Equal(new StatValue(Stat.Vita, "2"), new UnitProfile { W = 2, Vita = 1 }.Vitality);
        Assert.Equal(new StatValue(Stat.Vita, "1"), new UnitProfile { Vita = 1 }.Vitality);
        Assert.Equal(new StatValue(Stat.Vita, "-"), new UnitProfile().Vitality);
    }
}
