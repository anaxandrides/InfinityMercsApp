using InfinityMercsApp.Domain.Fireteams;

namespace InfinityMercsApp.UnitTests.Fireteams;

public sealed class FireteamMatchingTests
{
    private sealed record TestUnit(string Name, string? Slug);

    [Theory]
    [InlineData("Kamau Hacker", "KAMAUHACKER")]
    [InlineData("Ñ-Señor Massacre", "NSENORMASSACRE")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void NormalizeTeamUnitName_KeepsUpperCaseLettersAndDigitsWithoutAccents(string? name, string expected) =>
        Assert.Equal(expected, FireteamMatching.NormalizeTeamUnitName(name));

    [Theory]
    [InlineData("reinf-orc_troops", "orc-troops")]
    [InlineData("Orc Troops-REINF", "orc-troops")]
    [InlineData("--fusilier--", "fusilier")]
    public void NormalizeSlugForLookup_DropsReinforcementMarkersAndPunctuation(string slug, string expected) =>
        Assert.Equal(expected, FireteamMatching.NormalizeSlugForLookup(slug));

    [Theory]
    [InlineData("Wildcard", null, true)]
    [InlineData("Orc", "wild-card", true)]
    [InlineData("Orc", "orc", false)]
    public void IsWildcardEntry_ChecksNameAndSlug(string name, string? slug, bool expected) =>
        Assert.Equal(expected, FireteamMatching.IsWildcardEntry(name, slug));

    [Theory]
    [InlineData("Fusilier FTO", true)]
    [InlineData("fto", true)]
    [InlineData("Lofton", false)]
    [InlineData(null, false)]
    public void IsFtoLabel_MatchesTheWholeWord(string? label, bool expected) =>
        Assert.Equal(expected, FireteamMatching.IsFtoLabel(label));

    [Fact]
    public void ResolveUnitForTeamEntry_PrefersSlugThenNormalisedSlugThenName()
    {
        List<TestUnit> units = [new("Orc Troops", "orc-troops"), new("Fusiliers", "fusiliers"), new("Bolts", null)];

        Assert.Same(units[0], Resolve("Anything", "ORC-TROOPS", units));
        Assert.Same(units[1], Resolve("Fusiliers", "reinf_fusiliers", units));
        Assert.Same(units[2], Resolve("bolts", null, units));
        Assert.Same(units[0], Resolve("Orc-Troops!", null, units));
        Assert.Null(Resolve("Nobody", "nobody", units));
    }

    [Fact]
    public void ResolveUnitForTeamEntry_PrefersReinforcementUnitsForReinforcementSlots()
    {
        List<TestUnit> units = [new("Orc Troops", "orc-troops"), new("Orc Troops REINF", "orc-troops-reinf")];

        Assert.Same(units[1], Resolve("Orc Troops REINF", "orc-troops", units));
        Assert.Same(units[0], Resolve("REINF Orc Troops", "orc-troops", [units[0]]));
    }

    private static TestUnit? Resolve(string? name, string? slug, IEnumerable<TestUnit> units) =>
        FireteamMatching.ResolveUnitForTeamEntry(name, slug, units, x => x.Name, x => x.Slug);
}
