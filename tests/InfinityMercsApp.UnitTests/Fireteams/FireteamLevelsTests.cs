using InfinityMercsApp.Domain.Fireteams;
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
    public void BonusText_Level_AccumulatesBonusesUpToIt(int level, string? expected) =>
        Assert.Equal(expected, FireteamLevels.BonusText(level));

    [Theory]
    [InlineData("3", 3)]
    [InlineData(" 2 ", 2)]
    [InlineData("0", 0)]
    [InlineData("x", 0)]
    [InlineData("*", int.MaxValue / 2)]
    [InlineData(null, int.MaxValue / 2)]
    public void MaxSlots_MaxText_ReadsTheSlotCount(string? max, int expected) =>
        Assert.Equal(expected, FireteamLevels.MaxSlots(max));

    [Fact]
    public void SlotNameKeys_NameWithAliases_IncludesAliasesAndPlurals() =>
        Assert.Equal(
            new HashSet<string> { "FUSILIER", "FUSILIERS", "ORC", "ORCS", "KAMAU", "KAMAUS" },
            FireteamLevels.SlotNameKeys("Fusiliers (Orc / Kamau)"));

    [Fact]
    public void ClaimSlots_MoreMembersThanMax_FillsEachSlotUpToMax()
    {
        List<FireteamSlot> slots = [new FireteamSlot("Fusilier", null, null, "2")];
        var members = Enumerable.Range(0, 3).Select(_ => new FireteamMember("Fusiliers", "Fusilier", 1, 1)).ToList();

        Assert.Equal<bool>([true, true, false], FireteamLevels.ClaimSlots(members, slots));
        Assert.Equal(2, FireteamLevels.Level(members, slots));
    }

    [Fact]
    public void ClaimSlots_NamesDiffer_MatchesByResolvedUnit()
    {
        List<FireteamSlot> slots = [new FireteamSlot("Something Else", 7, 3, "1")];
        Assert.Equal<bool>([true, false], FireteamLevels.ClaimSlots(
            [new FireteamMember("Orc", "Orc Troops", 7, 3), new FireteamMember("Orc", "Orc Troops", 7, 4)],
            slots));
    }

    [Fact]
    public void ClaimSlots_NoSlots_LeavesEveryoneIrregular()
    {
        Assert.Equal<bool>([false], FireteamLevels.ClaimSlots([new FireteamMember("Orc", null, 1, 1)], []));
        Assert.Equal(0, FireteamLevels.Level([new FireteamMember("Orc", null, 1, 1)], []));
    }

    [Fact]
    public void Level_MoreThanSixMembers_IsCappedAtSix()
    {
        var members = Enumerable.Range(0, 8).Select(_ => new FireteamMember("Orc", null, 1, 1)).ToList();
        Assert.Equal(FireteamLevels.MaxLevel, FireteamLevels.Level(members, [new FireteamSlot("Orc", null, null, "*")]));
    }

    public static TheoryData<int> Seeds() => new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [Theory]
    [MemberData(nameof(Seeds))]
    public void LevelAndClaimSlots_RandomTeams_MatchLegacyEvaluator(int seed)
    {
        var random = new Random(seed);
        List<string> names = ["Orc Troops", "Orcs", "Fusilier", "Kamau (Hacker)", "Bolts", "Wildcard", "Moderator", ""];
        List<string> maxes = ["1", "2", "3", "*", "0", "", "x"];

        var slots = Enumerable.Range(0, random.Next(0, 5))
            .Select(_ => new FireteamSlot(
                $"{names[random.Next(names.Count)]}{(random.Next(3) == 0 ? " (Orc, Bolt)" : "")}",
                random.Next(3) == 0 ? random.Next(1, 4) : null,
                random.Next(3) == 0 ? random.Next(1, 3) : null,
                maxes[random.Next(maxes.Count)]))
            .ToList();
        var members = Enumerable.Range(0, random.Next(0, 9))
            .Select(_ => new FireteamMember(names[random.Next(names.Count)], names[random.Next(names.Count)], random.Next(1, 4), random.Next(1, 3)))
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
            members.Select(m => expectedIrregular[m]),
            FireteamLevels.ClaimSlots(members, slots).Select(claimed => !claimed));
    }
}
