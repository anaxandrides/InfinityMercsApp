using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.UnitTests.Company;

public sealed class SavedUnitTextTests
{
    [Theory]
    [InlineData("Fusilier (Lieutenant)", "Fusilier")]
    [InlineData("Bolt  (Hacker)  Team (2)", "Bolt Team")]
    [InlineData("(Specialist)", "Unit")]
    [InlineData(null, "Unit")]
    public void BaseDisplayNameDropsBracketedParts(string? name, string expected) =>
        Assert.Equal(expected, SavedUnitText.BaseDisplayName(name));

    [Theory]
    [InlineData(" hi ", "HI")]
    [InlineData("MOV", "")]
    [InlineData(null, "")]
    public void UnitTypeCodeIsUpperCaseAndNeverMov(string? code, string expected) =>
        Assert.Equal(expected, SavedUnitText.UnitTypeCode(code));

    [Fact]
    public void ExperienceIconsStartAtTheFirstRank()
    {
        Assert.Equal(string.Empty, SavedUnitText.ExperienceIconPath(0));
        Assert.EndsWith("-stars.svg", SavedUnitText.ExperienceIconPath(1000));
    }

    [Fact]
    public void CaptainChoiceNamesLoseCostsBlanksAndRepeats() =>
        Assert.Equal(
            ["Sniper Rifle", "Grenades"],
            SavedUnitText.CaptainChoiceNames("(2) - Sniper Rifle", null, "-", " ", "sniper rifle", "Grenades"));

    [Fact]
    public void SectionKeepsOneOfEachLine()
    {
        Assert.Equal(string.Join(Environment.NewLine, "Combi Rifle", "Pistol"), SavedUnitText.Section("Combi Rifle\nPistol\ncombi rifle"));
        Assert.Equal("-", SavedUnitText.Section(" - "));
    }
}
