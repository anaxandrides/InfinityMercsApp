using InfinityMercsApp.Domain.Fireteams;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Fireteams;

public sealed class FireteamLevelsTests
{
    [Theory]
    [InlineData(0, null)]
    [InlineData(1, null)]
    [InlineData(2, "BS Attack (+1 SD)")]
    [InlineData(4, "BS Attack (+1 SD), +3 Discover and +1 Dodge, +1 BS")]
    [InlineData(6, "BS Attack (+1 SD), +3 Discover and +1 Dodge, +1 BS, Sixth Sense, +1 WIP")]
    [InlineData(7, null)]
    public void BonusText_AccumulatesUpToTheLevel(int level, string? expected) =>
        Assert.Equal(expected, FireteamLevels.BonusText(level));

    [Theory]
    [InlineData("3", 3)]
    [InlineData(" 2 ", 2)]
    [InlineData("0", 0)]
    [InlineData("x", 0)]
    [InlineData("*", int.MaxValue / 2)]
    [InlineData(null, int.MaxValue / 2)]
    public void MaxSlots_ReadsTheSlotCount(string? max, int expected) =>
        Assert.Equal(expected, FireteamLevels.MaxSlots(max));

    [Fact]
    public void SlotNameKeys_IncludeAliasesAndPluralVariants() =>
        Assert.Equal(
            new HashSet<string> { "FUSILIER", "FUSILIERS", "ORC", "ORCS", "KAMAU", "KAMAUS" },
            FireteamLevels.SlotNameKeys("Fusiliers (Orc / Kamau)"));

    [Fact]
    public void ClaimSlots_FillEachSlotUpToItsMax()
    {
        var slots = new[] { new FireteamSlot("Fusilier", null, null, "2") };
        var members = Enumerable.Range(0, 3).Select(_ => new FireteamMember("Fusiliers", "Fusilier", 1, 1)).ToList();

        Assert.Equal(new[] { true, true, false }, FireteamLevels.ClaimSlots(members, slots));
        Assert.Equal(2, FireteamLevels.Level(members, slots));
    }

    [Fact]
    public void ClaimSlots_MatchByResolvedUnitWhenNamesDiffer()
    {
        var slots = new[] { new FireteamSlot("Something Else", 7, 3, "1") };
        Assert.Equal(new[] { true, false }, FireteamLevels.ClaimSlots(
            [new FireteamMember("Orc", "Orc Troops", 7, 3), new FireteamMember("Orc", "Orc Troops", 7, 4)],
            slots));
    }

    [Fact]
    public void ClaimSlots_WithoutSlotsLeaveEveryoneIrregular()
    {
        Assert.Equal(new[] { false }, FireteamLevels.ClaimSlots([new FireteamMember("Orc", null, 1, 1)], []));
        Assert.Equal(0, FireteamLevels.Level([new FireteamMember("Orc", null, 1, 1)], []));
    }

    [Fact]
    public void Level_IsCappedAtSix()
    {
        var members = Enumerable.Range(0, 8).Select(_ => new FireteamMember("Orc", null, 1, 1)).ToList();
        Assert.Equal(FireteamLevels.MaxLevel, FireteamLevels.Level(members, [new FireteamSlot("Orc", null, null, "*")]));
    }

    public static TheoryData<int> Seeds() => new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [Theory]
    [MemberData(nameof(Seeds))]
    public void LevelAndIrregularStatus_MatchTheLegacyEvaluator(int seed)
    {
        var random = new Random(seed);
        string[] names = ["Orc Troops", "Orcs", "Fusilier", "Kamau (Hacker)", "Bolts", "Wildcard", "Moderator", ""];
        string[] maxes = ["1", "2", "3", "*", "0", "", "x"];

        var slots = Enumerable.Range(0, random.Next(0, 5))
            .Select(_ => new FireteamSlot(
                $"{names[random.Next(names.Length)]}{(random.Next(3) == 0 ? " (Orc, Bolt)" : "")}",
                random.Next(3) == 0 ? random.Next(1, 4) : null,
                random.Next(3) == 0 ? random.Next(1, 3) : null,
                maxes[random.Next(maxes.Length)]))
            .ToList();
        var members = Enumerable.Range(0, random.Next(0, 9))
            .Select(_ => new FireteamMember(names[random.Next(names.Length)], names[random.Next(names.Length)], random.Next(1, 4), random.Next(1, 3)))
            .ToList();

        var expectedLevel = Math.Clamp(LegacyFireteamLevelWorkflow.EvaluateLevelByCapacity(
            members, slots,
            m => m.BaseUnitName, m => m.Name, m => m.UnitId, m => m.FactionId,
            s => s.Name, s => s.UnitId, s => s.FactionId, s => s.Max ?? string.Empty), 0, 6);
        var expectedIrregular = new Dictionary<FireteamMember, bool>(ReferenceEqualityComparer.Instance);
        LegacyFireteamLevelWorkflow.EvaluateIrregularStatus(
            members, slots,
            m => m.BaseUnitName, m => m.Name, m => m.UnitId, m => m.FactionId,
            s => s.Name, s => s.UnitId, s => s.FactionId, s => s.Max ?? string.Empty,
            (m, irregular) => expectedIrregular[m] = irregular);

        Assert.Equal(expectedLevel, FireteamLevels.Level(members, slots));
        Assert.Equal(
            members.Select(m => expectedIrregular[m]).ToArray(),
            FireteamLevels.ClaimSlots(members, slots).Select(claimed => !claimed).ToArray());
    }
}

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
        TestUnit[] units = [new("Orc Troops", "orc-troops"), new("Fusiliers", "fusiliers"), new("Bolts", null)];

        Assert.Same(units[0], Resolve("Anything", "ORC-TROOPS", units));
        Assert.Same(units[1], Resolve("Fusiliers", "reinf_fusiliers", units));
        Assert.Same(units[2], Resolve("bolts", null, units));
        Assert.Same(units[0], Resolve("Orc-Troops!", null, units));
        Assert.Null(Resolve("Nobody", "nobody", units));
    }

    [Fact]
    public void ResolveUnitForTeamEntry_PrefersReinforcementUnitsForReinforcementSlots()
    {
        TestUnit[] units = [new("Orc Troops", "orc-troops"), new("Orc Troops REINF", "orc-troops-reinf")];

        Assert.Same(units[1], Resolve("Orc Troops REINF", "orc-troops", units));
        Assert.Same(units[0], Resolve("REINF Orc Troops", "orc-troops", [units[0]]));
    }

    private static TestUnit? Resolve(string? name, string? slug, IEnumerable<TestUnit> units) =>
        FireteamMatching.ResolveUnitForTeamEntry(name, slug, units, x => x.Name, x => x.Slug);
}

public sealed class MergedFireteamTests
{
    [Fact]
    public void MergeChart_AddsCountsAndWidensSlotLimitsAcrossCharts()
    {
        var first = new FireteamChart
        {
            Teams =
            [
                new FireteamTeam
                {
                    Name = " Kestrel ",
                    TypeList = ["CORE", "HARIS"],
                    Units = [new FireteamUnitLimit { Name = "Orc", Min = 1, Max = 2 }]
                },
                new FireteamTeam { Name = " ", TypeList = ["DUO"] }
            ]
        };
        var second = new FireteamChart
        {
            Teams =
            [
                new FireteamTeam
                {
                    Name = "kestrel",
                    TypeList = ["CORE", "DUO"],
                    Units = [new FireteamUnitLimit { Name = "ORC", Slug = "orc", Min = 0, Max = 3, MinIsAsterisk = true }]
                }
            ]
        };

        var teams = new Dictionary<string, MergedFireteam>(StringComparer.OrdinalIgnoreCase);
        MergedFireteam.MergeChart(first, teams);
        MergedFireteam.MergeChart(second, teams);
        MergedFireteam.MergeChart(null, teams);

        var team = Assert.Single(teams.Values);
        Assert.Equal(("Kestrel", 1, 1, 2), (team.Name, team.Duo, team.Haris, team.Core));
        Assert.Equal((0, 3, "orc", true), Assert.Single(team.UnitLimits).Value);
    }
}
