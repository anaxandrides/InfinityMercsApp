using InfinityMercsApp.Domain.Season.RecruitRolls;

namespace InfinityMercsApp.UnitTests.Season.RecruitRolls;

public sealed class RolledArmorTests
{
    [Theory]
    [InlineData("Chemtank", "Regeneration")]
    [InlineData("Irmandihno Wetsuit (LI)", "Mimetism (-6)")]
    public void Name_RolledCopy_ParsesBackToTheSameArmor(string armor, string result)
    {
        var name = new RolledArmor(armor, result).Name;

        Assert.Equal(new RolledArmor(armor, result), RolledArmor.Parse(name));
    }

    [Theory]
    [InlineData("Chemtank")]
    [InlineData("Light Armour (LI)")]
    public void Parse_NoResult_ReturnsNull(string name) => Assert.Null(RolledArmor.Parse(name));
}
