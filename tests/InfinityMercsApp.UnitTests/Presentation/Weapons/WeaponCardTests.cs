using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Presentation.Weapons;

namespace InfinityMercsApp.UnitTests.Presentation.Weapons;

public sealed class WeaponCardTests
{
    private static Weapon Rifle() => new()
    {
        Name = "Combi Rifle",
        Burst = "3",
        Damage = "13",
        Saving = "ARM",
        SavingNum = "1",
        Properties = ["Suppressive Fire", "[***]", "Anti-materiel", "Continuous Damage"]
    };

    [Fact]
    public void StatsSplitIntoOffenceAndSaving()
    {
        var card = WeaponCard.From(Rifle(), ammo: "AP");

        Assert.Equal([new("Burst", "3"), new("Damage", "13"), new("Ammo", "AP")], card.LeftStats);
        Assert.Equal([new("Saving", "ARM"), new("Saving Rolls", "1")], card.RightStats);
        Assert.True(card.HasStats);
        Assert.False(card.HasMode);
    }

    [Fact]
    public void BurstBonusAndDamageReductionAdjustTheNumbers()
    {
        var card = WeaponCard.From(Rifle(), damageReduction: 20, burstBonus: 1);

        Assert.Equal([new("Burst", "4"), new("Damage", "1")], card.LeftStats);
    }

    [Fact]
    public void ValuesThatAreNotNumbersAreLeftAlone()
    {
        var weapon = Rifle();
        weapon.Burst = "2+1";
        weapon.Damage = "PH";

        var card = WeaponCard.From(weapon, damageReduction: 2, burstBonus: 1);

        Assert.Equal([new("Burst", "2+1"), new("Damage", "PH")], card.LeftStats);
    }

    [Fact]
    public void DashesAreLeftOff()
    {
        var card = WeaponCard.From(new Weapon { Burst = "-", Damage = " ", Saving = "-" });

        Assert.False(card.HasStats);
        Assert.False(card.HasSpecialRules);
    }

    [Fact]
    public void RulesPairUpWithWikiLinksAndSkipPlaceholders()
    {
        var card = WeaponCard.From(Rifle());

        Assert.Equal(2, card.RuleRows.Count);
        Assert.Equal(("Suppressive Fire", "Anti-materiel"), (card.RuleRows[0].Left.Text, card.RuleRows[0].Right?.Text));
        Assert.Equal("https://infinitythewiki.com/Suppressive_Fire?version=n4", card.RuleRows[0].Left.Url);
        Assert.False(card.RuleRows[1].HasRight);
    }

    [Fact]
    public void TheFiringModeHeadsTheCard() =>
        Assert.Equal("Anti-materiel Mode", WeaponCard.From(new Weapon { Mode = " Anti-materiel Mode " }).Mode);
}
