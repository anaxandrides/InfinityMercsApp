using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.RecruitRolls;

public sealed class RollResultsTests
{
    private static readonly GainKinds Kinds = new(
        ["HMG", "DA CC Weapon", "Light Flamethrower"],
        ["Mimetism", "Regeneration", "Dogged", "No Wound Incapacitation", "Immunity"]);

    private static TrooperProfile Line(IReadOnlyList<string>? skills = null) =>
        new("10-10", "14", "12", "10", "13", "3", "0", "1", "2", skills ?? ["Courage"], ["Regular"]);

    [Fact]
    public void Apply_StatBonuses_AddsThemOnTop()
    {
        var rolled = RollResults.Apply(Line(), "LI", ["+1 ARM", "+3 PH"], Kinds);

        Assert.Equal(("4", "13"), (rolled.Profile.Arm, rolled.Profile.Ph));
    }

    [Fact]
    public void Apply_NamedResults_SortsIntoWeaponsSkillsAndEquipment()
    {
        var rolled = RollResults.Apply(Line(), "LI", ["DA CC Weapon", "Regeneration", "Multispectral Visor L1"], Kinds);

        Assert.Equal(["DA CC Weapon"], rolled.Weapons);
        Assert.Equal(["Multispectral Visor L1"], rolled.Equipment);
        Assert.Equal(["Courage", "Regeneration"], rolled.Profile.Skills);
    }

    [Fact]
    public void Apply_ModifiedSkillHeld_AddsIntoIt()
    {
        Assert.Equal(["Mimetism (-9)"], RollResults.Apply(Line(["Mimetism (-3)"]), "LI", ["Mimetism (-6)"], Kinds).Profile.Skills);
    }

    [Fact]
    public void Apply_DoggedWithNwi_DoesNothing()
    {
        Assert.Equal(
            ["No Wound Incapacitation", "Immunity (Enhanced)"],
            RollResults.Apply(Line(["No Wound Incapacitation"]), "LI", ["Dogged + Immunity (Enhanced)"], Kinds).Profile.Skills);
    }

    [Fact]
    public void Apply_NwiWithDogged_ReplacesDogged()
    {
        Assert.Equal(["No Wound Incapacitation"], RollResults.Apply(Line(["Dogged"]), "LI", ["No Wound Incapacitation"], Kinds).Profile.Skills);
    }

    [Fact]
    public void Apply_TagTrooper_TakesTheTagSide()
    {
        var rolled = RollResults.Apply(Line([]), "TAG", ["TAG: BS Attack (+1B) / Other Troop Types: HMG"], new GainKinds(["HMG"], ["BS Attack"]));

        Assert.Empty(rolled.Weapons);
        Assert.Equal(["BS Attack (+1B)"], rolled.Profile.Skills);
    }
}
