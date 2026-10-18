using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.UnitTests.Company;

public sealed class SavedUnitTextTests
{
    [Theory]
    [InlineData("Fusilier (Lieutenant)", "Fusilier")]
    [InlineData("Bolt  (Hacker)  Team (2)", "Bolt Team")]
    [InlineData("(Specialist)", "Unit")]
    [InlineData(null, "Unit")]
    public void BaseDisplayName_BracketedParts_DropsThem(string? name, string expected) =>
        Assert.Equal(expected, SavedUnitText.BaseDisplayName(name));

    [Theory]
    [InlineData(" hi ", "HI")]
    [InlineData("MOV", "")]
    [InlineData(null, "")]
    public void UnitTypeCode_Code_IsUpperCaseAndNeverMov(string? code, string expected) =>
        Assert.Equal(expected, SavedUnitText.UnitTypeCode(code));

    [Fact]
    public void ExperienceIconPath_Experience_StartsAtTheFirstRank()
    {
        Assert.Equal(string.Empty, SavedUnitText.ExperienceIconPath(0));
        Assert.EndsWith("-stars.svg", SavedUnitText.ExperienceIconPath(1000));
    }

    [Fact]
    public void CaptainChoiceNames_CostsBlanksAndRepeats_DropsThem() =>
        Assert.Equal(
            ["Sniper Rifle", "Grenades"],
            SavedUnitText.CaptainChoiceNames("(2) - Sniper Rifle", null, "-", " ", "sniper rifle", "Grenades"));

    [Fact]
    public void Section_RepeatedLines_KeepsOneOfEach()
    {
        Assert.Equal(string.Join(Environment.NewLine, "Combi Rifle", "Pistol"), SavedUnitText.Section("Combi Rifle\nPistol\ncombi rifle"));
        Assert.Equal("-", SavedUnitText.Section(" - "));
    }
}
