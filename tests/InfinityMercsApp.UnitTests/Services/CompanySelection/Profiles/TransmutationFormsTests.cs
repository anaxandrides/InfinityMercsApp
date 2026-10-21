using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Services.CompanySelection.Profiles;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Profiles;

public sealed class TransmutationFormsTests
{
    private const int Transmutation = 246;
    private const int Dodge = 30;
    private const int BsAttack = 31;
    private const int Auto = 254;

    internal static readonly ArmyFilterCatalog Filters = new(new Dictionary<string, IReadOnlyList<ArmyFilterItem>>
    {
        [ArmyFilterSection.Skills] =
        [
            new ArmyFilterItem(Transmutation, "Transmutation", null, null),
            new ArmyFilterItem(Dodge, "Dodge", null, null),
            new ArmyFilterItem(BsAttack, "BS Attack", null, null)
        ],
        [ArmyFilterSection.Extras] = [new ArmyFilterItem(Auto, "Auto", null, null)]
    });

    private static readonly ProfileEntry TransmutationAuto = new(Transmutation, 1, null, [Auto]);

    // A Su-Jian: two forms in one group, each with skills of its own.
    internal static readonly UnitProfileGroups SuJian = new(
    [
        new ProfileGroup
        {
            Id = 1,
            Profiles =
            [
                new UnitProfile { Name = "HIGH MOBILITY FORM", MoveCm = [20, 5], Cc = 9, Arm = 5, Skills = [TransmutationAuto, ProfileEntry.ForId(Dodge)] },
                new UnitProfile { Name = "COMBAT FORM", MoveCm = [10, 10], Cc = 17, Arm = 5, Skills = [TransmutationAuto, ProfileEntry.ForId(BsAttack)] }
            ]
        },
        new ProfileGroup { Id = 2, Profiles = [new UnitProfile { Name = "OTHER" }] }
    ]);

    [Theory]
    [InlineData("Transmutation (Auto)", true)]
    [InlineData("Transmutation (WIP)", false)]
    [InlineData("Dodge", false)]
    [InlineData(null, false)]
    public void IsAuto_Skill_RecognisesTransmutationAuto(string? skill, bool expected) =>
        Assert.Equal(expected, TransmutationForms.IsAuto(skill));

    [Fact]
    public void FormsOf_GroupWithSeveralProfiles_ReturnsThem() =>
        Assert.Equal(["HIGH MOBILITY FORM", "COMBAT FORM"], TransmutationForms.FormsOf(SuJian, 1).Select(form => form.Name));

    [Fact]
    public void FormsOf_GroupWithOneProfile_ReturnsNone() =>
        Assert.Empty(TransmutationForms.FormsOf(SuJian, 2));

    [Fact]
    public void Of_Form_NamesItsOwnSkillsWithoutTransmutation()
    {
        var form = TransmutationForms.Of(SuJian.Groups[0].Profiles[1], Filters, showUnitsInInches: false);

        Assert.Equal(["BS Attack"], form.Skills);
        Assert.Same(SuJian.Groups[0].Profiles[1], form.Profile);
    }
}
