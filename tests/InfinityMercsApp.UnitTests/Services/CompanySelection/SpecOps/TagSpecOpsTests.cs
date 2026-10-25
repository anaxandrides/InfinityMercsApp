using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.SpecOps;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.SpecOps;

/// <summary>A TAG's Spec-Ops come from the faction beside the TAG company, and leave out what a TAG may never take.</summary>
public sealed class TagSpecOpsTests
{
    private const int TagCompany = 2003;

    [Theory]
    [InlineData(TagCompany, 101, 102, 102)]
    [InlineData(101, 102, null, 101)]
    [InlineData(TagCompany, 102, TagCompany, 102)]
    [InlineData(0, 102, null, 102)]
    [InlineData(TagCompany, TagCompany, null, TagCompany)]
    [InlineData(TagCompany, null, null, TagCompany)]
    public void SourceFactionId_Factions_PrefersOneBesideTheTagCompany(int trooper, int? firstSource, int? preferred, int expected) =>
        Assert.Equal(expected, TagSpecOps.SourceFactionId(new CaptainFactions(trooper, firstSource, [], preferred)));

    [Theory]
    [InlineData(199, 101)]
    [InlineData(1099, 1001)]
    [InlineData(101, null)]
    [InlineData(0, null)]
    [InlineData(-99, null)]
    public void VanillaOfReinforcement_FactionId_IsItsVanillaFaction(int factionId, int? expected) =>
        Assert.Equal(expected, TagSpecOps.VanillaOfReinforcement(factionId));

    [Fact]
    public void WithoutDisallowed_Options_DropsBannedSkillsEverywhere()
    {
        var options = TagSpecOps.WithoutDisallowed(new CaptainUpgradeOptionSet
        {
            Weapons = ["(5) - Spitfire"],
            Skills = ["(10) - Infiltration", "(3) - Courage"],
            Equipment = ["(4) - Engineer (+Gizmokit)", "(2) - MediKit"]
        });

        Assert.Equal(["(5) - Spitfire"], options.Weapons);
        Assert.Equal(["(3) - Courage"], options.Skills);
        Assert.Equal(["(2) - MediKit"], options.Equipment);
    }
}
