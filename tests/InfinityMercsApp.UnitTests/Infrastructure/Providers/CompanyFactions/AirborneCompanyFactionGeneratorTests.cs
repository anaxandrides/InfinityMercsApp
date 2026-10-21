using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

namespace InfinityMercsApp.UnitTests.Infrastructure.Providers.CompanyFactions;

/// <summary>Only troopers that deploy airborne may join an Airborne company, whether the profile or the loadout gives the skill.</summary>
public sealed class AirborneCompanyFactionGeneratorTests
{
    private const int CombatJump = 35;
    private const int Parachutist = 33;
    private const int Camouflage = 28;

    private static readonly HashSet<int> AirborneSkills = [CombatJump, Parachutist];

    [Fact]
    public void AirborneOptions_ProfileDeploysAirborne_KeepsEveryOptionWithoutSwc()
    {
        var group = Group([CombatJump], Option(1, swc: "0"), Option(2, swc: "0.5"), Option(3, swc: "+1"));

        var options = AirborneCompanyFactionGenerator.AirborneOptions(group, AirborneSkills);

        Assert.Equal([1, 3], options.Select(option => option.Id));
    }

    // Crushers: only one of their loadouts has Combat Jump.
    [Fact]
    public void AirborneOptions_OneLoadoutDeploysAirborne_KeepsOnlyThatOne()
    {
        var group = Group([Camouflage], Option(1, swc: "0"), Option(4, swc: "0", CombatJump, Parachutist));

        var options = AirborneCompanyFactionGenerator.AirborneOptions(group, AirborneSkills);

        Assert.Equal(4, Assert.Single(options).Id);
    }

    [Fact]
    public void AirborneOptions_NoAirborneSkill_KeepsNothing()
    {
        var group = Group([Camouflage], Option(1, swc: "0", Camouflage));

        Assert.Empty(AirborneCompanyFactionGenerator.AirborneOptions(group, AirborneSkills));
    }

    private static ProfileGroup Group(IReadOnlyList<int> profileSkills, params IEnumerable<ProfileOption> options) => new()
    {
        Profiles = [new UnitProfile { Skills = profileSkills.Select(ProfileEntry.ForId).ToList() }],
        Options = options.ToList()
    };

    private static ProfileOption Option(int id, string swc, params IEnumerable<int> skills) => new()
    {
        Id = id,
        Swc = swc,
        Skills = skills.Select(ProfileEntry.ForId).ToList()
    };
}
