using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.UnitTests.Company;

public sealed class TagRestrictionsTests
{
    [Theory]
    [InlineData("Engineer (+Gizmokit)", "Engineer")]
    [InlineData("(3) - Combat Jump (-3)", "Combat Jump")]
    [InlineData("Forward Deployment (+8\")", "Forward Deployment")]
    [InlineData("Shasvastii, Impersonation", "Impersonation")]
    [InlineData("Courage", null)]
    [InlineData(null, null)]
    public void BannedSkillIn_Name_ReturnsTheBannedSkillItGives(string? name, string? expected) =>
        Assert.Equal(expected, TagRestrictions.BannedSkillIn(name));
}
