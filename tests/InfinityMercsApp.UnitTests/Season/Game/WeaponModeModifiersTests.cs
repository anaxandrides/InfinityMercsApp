using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.UnitTests.Season.Game;

public sealed class WeaponModeModifiersTests
{
    private static readonly WeaponModeModifiers Card = new(2, "Shock", 1);

    private static Weapon Weapon(string? mode = null, params IReadOnlyList<string> properties) =>
        new() { Name = "Combi Rifle", Mode = mode, Burst = "3", Damage = "13", Properties = properties };

    [Fact]
    public void AnOrdinaryModeGetsEverything()
    {
        var weapon = Weapon();

        Assert.Equal(Card, WeaponModeModifiers.For(weapon, [weapon], "Combi Rifle", Card));
    }

    [Fact]
    public void DeployedAndWipModesGetNothing()
    {
        var deployable = Weapon("Deployable Mode");
        var wip = Weapon(null, "BS Weapon (WIP)");

        Assert.Equal(WeaponModeModifiers.None, WeaponModeModifiers.For(deployable, [deployable], "Drop Bears", Card));
        Assert.Equal(WeaponModeModifiers.None, WeaponModeModifiers.For(wip, [wip], "Pitcher", Card));
    }

    [Fact]
    public void TheOtherModesOfADeployableWeaponOnlyGetTheBurstBonus()
    {
        var thrown = Weapon("BS Mode");
        var deployable = Weapon("Deployable Mode");

        Assert.Equal(new WeaponModeModifiers(0, null, 1), WeaponModeModifiers.For(thrown, [thrown, deployable], "Drop Bears", Card));
    }

    [Fact]
    public void SuppressiveFireOnlyKeepsTheDamageReduction()
    {
        var weapon = Weapon();

        Assert.Equal(new WeaponModeModifiers(2, null, 0), WeaponModeModifiers.For(weapon, [weapon], "Suppressive Fire Mode", Card));
    }

    [Theory]
    [InlineData("3", 1, "4")]
    [InlineData("-", 1, "-")]
    [InlineData("3", 0, "3")]
    public void BurstGetsTheBonus(string burst, int bonus, string expected) =>
        Assert.Equal(expected, (WeaponModeModifiers.None with { BurstBonus = bonus }).Burst(burst));

    [Theory]
    [InlineData("13", 2, "11")]
    [InlineData("2", 5, "1")]
    [InlineData("PH", 2, "PH")]
    public void DamageIsReducedButNeverBelowOne(string damage, int reduction, string expected) =>
        Assert.Equal(expected, (WeaponModeModifiers.None with { DamageReduction = reduction }).Damage(damage));

    [Theory]
    [InlineData(null, "Shock")]
    [InlineData("N", "Shock")]
    [InlineData("DA", "DA+Shock")]
    [InlineData("Shock", "Shock")]
    public void AmmoCombinesWithTheModifier(string? baseAmmo, string expected) =>
        Assert.Equal(expected, (WeaponModeModifiers.None with { AmmoModifier = "Shock" }).Ammo(baseAmmo));

    [Fact]
    public void AmmoWithoutAModifierIsUnchanged() => Assert.Equal("AP", WeaponModeModifiers.None.Ammo("AP"));
}
