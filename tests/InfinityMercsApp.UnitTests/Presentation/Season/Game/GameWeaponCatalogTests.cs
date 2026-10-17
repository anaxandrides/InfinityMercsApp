using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Metadata.Ranges;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Presentation.Season.Game;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Presentation.Season.Game;

public sealed class GameWeaponCatalogTests
{
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();

    public GameWeaponCatalogTests()
    {
        _metadata.SearchWeaponsByName(Arg.Any<string>()).Returns([]);
        _metadata.GetAmmunitions().Returns([new Ammunition { Id = 1, Name = "N" }, new Ammunition { Id = 2, Name = "DA" }]);
        Returns("Combi Rifle", new Weapon { Name = "Combi Rifle", Burst = "3", Damage = "13", AmmunitionId = 1, Distance = new WeaponDistance() });
        Returns("Pistol", new Weapon { Name = "Pistol", Burst = "2", Damage = "11", AmmunitionId = 1 });
        Returns("Knife", new Weapon { Name = "Knife", Damage = "PH-1", AmmunitionId = 1 });
        Returns("Discover", new Weapon { Name = "Discover", Burst = "1" });
        Returns("Suppressive Fire Mode Weapon", new Weapon { Name = "Suppressive Fire Mode Weapon", Burst = "3", Damage = "13", AmmunitionId = 2 });
    }

    private void Returns(string name, params IReadOnlyList<Weapon> weapons) => _metadata.SearchWeaponsByName(name).Returns(weapons);

    [Fact]
    public void CardsAreBandedThenPlainThenUnknownThenCcThenStandardActions()
    {
        var cards = new GameWeaponCatalog(_metadata).CardsFor("Pistol\nZap Gun\nCombi Rifle (+1B)", "Knife", AttackModifiers.None);

        Assert.Equal(
            ["Combi Rifle (+1B)", "Pistol", "Zap Gun", GameWeaponCatalog.CcCardTitle, "Discover", WeaponModeModifiers.SuppressiveFireMode],
            cards.Select(card => card.Title));
        Assert.False(cards[2].IsMatched);
        Assert.True(cards[0].HasRange);
        Assert.False(cards[1].HasRange);
    }

    [Fact]
    public void BsAttackModifiersReachRangedWeaponsButNotDiscover()
    {
        var modifiers = new AttackModifiers(true, 2, "Shock", 1, 0);

        var cards = new GameWeaponCatalog(_metadata).CardsFor("Combi Rifle", null, modifiers);

        var combi = Assert.Single(cards[0].Modes);
        Assert.Equal(("4", "11", "Shock"), (combi.Burst, combi.Damage, combi.Ammo));
        Assert.True(cards[0].HasXVisor);
        Assert.Equal(WeaponModeModifiers.None, cards.Single(card => card.Title == "Discover").Modes[0].Modifiers);
        var suppressive = cards.Single(card => card.Title == WeaponModeModifiers.SuppressiveFireMode).Modes[0];
        Assert.Equal(("3", "11", "DA"), (suppressive.Burst, suppressive.Damage, suppressive.Ammo));
    }

    [Fact]
    public void CcAttackReducesCcWeaponDamage()
    {
        var cards = new GameWeaponCatalog(_metadata).CardsFor(null, "Knife", new AttackModifiers(false, 0, null, 0, 3));

        var cc = cards.Single(card => card.Title == GameWeaponCatalog.CcCardTitle);
        Assert.Equal(3, cc.Modes[0].Modifiers.DamageReduction);
        Assert.True(cc.Modes[0].IsFirst);
    }

    [Fact]
    public void WithoutMetadataEveryWeaponIsUnmatched()
    {
        var cards = new GameWeaponCatalog(null).CardsFor("Combi Rifle", "Knife", AttackModifiers.None);

        Assert.Equal(["Combi Rifle", "Knife"], cards.Select(card => card.Title));
        Assert.All(cards, card => Assert.False(card.IsMatched));
    }
}
