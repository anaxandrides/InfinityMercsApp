using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Downtime;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class DowntimeEventsTests
{
    [Theory]
    [InlineData(Stat.Cc, -10, "CC-10")]
    [InlineData(Stat.Ph, 0, "PH")]
    [InlineData(Stat.Wip, 3, "WIP+3")]
    public void Display_StatTest_ReadsAsKeyAndModifier(Stat stat, int modifier, string expected) =>
        Assert.Equal(expected, DowntimeTest.For(stat, modifier).Display);

    [Fact]
    public void Display_FixedTests_ReadAsPs10()
    {
        Assert.Equal("PS=10", DowntimeTest.Ps10.Display);
        Assert.Equal("PS=10+ARM", DowntimeTest.Ps10PlusArm.Display);
        Assert.False(DowntimeTest.Ps10.NeedsParticipant);
        Assert.True(DowntimeTest.Ps10PlusArm.NeedsParticipant);
    }

    [Fact]
    public void Display_Traits_ReadAsTheirRuleNames()
    {
        Assert.Equal("XP", new DowntimeTrait(TraitKind.Xp).Display);
        Assert.Equal("Chaotic", new DowntimeTrait(TraitKind.Chaotic).Display);
        Assert.Equal("CR (Neg)", new DowntimeTrait(TraitKind.Cr, Negated: true).Display);
        Assert.Equal("Skill (Stealth)", new DowntimeTrait(TraitKind.Skill, "Stealth").Display);
        Assert.Equal("Requirement (Hacker)", new DowntimeTrait(TraitKind.Requirement, "Hacker").Display);
        Assert.Equal("Take cover", new DowntimeTrait(TraitKind.Note, "Take cover").Display);
    }

    [Fact]
    public void LookupByRoll_One_OffersThreeTests()
    {
        var copEvent = DowntimeEventTable.LookupByRoll(1);

        Assert.Equal(["CC-10", "PH", "WIP"], copEvent.Choices.Select(c => c.TestLabel));
        Assert.Equal("Attack, Weapon, Chaotic, XP", string.Join(", ", copEvent.Choices[0].Traits.Select(t => t.Display)));
    }

    [Fact]
    public void AllEvents_EveryIncidentChoice_HasATest() =>
        Assert.All(DowntimeEventTable.AllEvents.SelectMany(e => e.Choices), choice => Assert.NotNull(choice.Test));

    [Fact]
    public void LookupByRoll_19Or20_IsNoIncident()
    {
        Assert.True(DowntimeEventTable.LookupByRoll(19).IsNoIncident);
        Assert.False(DowntimeEventTable.LookupByRoll(18).IsNoIncident);
    }
}
