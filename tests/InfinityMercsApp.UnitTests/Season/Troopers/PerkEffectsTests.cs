using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Troopers;

public sealed class PerkEffectsTests
{
    [Fact]
    public void AttributeBonusesChangeTheStatLine()
    {
        Assert.Equal(1, PerkEffects.FromName("+1 BS").Bs);
        Assert.Equal(3, PerkEffects.FromName("+3 CC").Cc);
        Assert.Equal(3, PerkEffects.FromName("+3 BTS").Bts);
    }

    [Fact]
    public void ABonusAndASkillSplitApart()
    {
        var change = PerkEffects.FromName("+2 PH + CC Attack (SR-2)");

        Assert.Equal(2, change.Ph);
        Assert.Equal(["CC Attack (SR-2)"], change.PerkSkills);
    }

    [Fact]
    public void SilhouetteAndArmourFromTheMechaTree()
    {
        var change = PerkEffects.FromName("S7, +1 ARM, SR-1");

        Assert.Equal((7, 1), (change.Silhouette, change.Arm));
        Assert.Equal(["SR-1"], change.PerkSkills);
    }

    [Fact]
    public void MoveAndWoundsSkill()
    {
        Assert.Equal(new MoveInches(6, 4), PerkEffects.FromName("MOV 6-4").Move);
        Assert.Equal(new MoveInches(8, 2), PerkEffects.FromName("Move 8-2").Move);
        Assert.Equal(1, PerkEffects.FromName("+1 Wounds Skill").WoundsSkillSteps);
    }

    [Fact]
    public void CommasInsideBracketsStayTogether()
    {
        Assert.Equal(["Dodge (+3)", "Dodge (+1\")"], PerkEffects.FromName("Dodge (+3), Dodge (+1\")").PerkSkills);
        Assert.Equal(["Immunity (DA)", "Immunity (Shock)"], PerkEffects.For("body-track-2-tier-1").PerkSkills);
    }

    [Fact]
    public void AnUnknownPerkChangesNothing()
    {
        Assert.Equal(TrooperChange.None, PerkEffects.For("nowhere-track-1-tier-1"));
    }
}
