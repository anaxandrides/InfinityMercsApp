using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Providers;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Metadata;

public sealed class WikiLinksTests
{
    [Theory]
    [InlineData("Mimetism (-6)", "https://infinitythewiki.com/Mimetism?version=n4")]
    [InlineData("Combi Rifle", "https://infinitythewiki.com/Combi_Rifle?version=n4")]
    [InlineData(" Climbing Plus ", "https://infinitythewiki.com/Climbing_Plus?version=n4")]
    public void LinksToThePageForTheNameBeforeAnyBracket(string name, string expected) =>
        Assert.Equal(expected, WikiLinks.ForName(name));

    [Theory]
    [InlineData("Combi Rifle (+1B)", "Combi Rifle")]
    [InlineData("Pistol", "Pistol")]
    [InlineData("(+1B)", "")]
    public void BaseNameDropsTheBracket(string name, string expected) => Assert.Equal(expected, WikiLinks.BaseName(name));
}

public sealed class SkillWikiLinksTests
{
    private static readonly SkillWikiLinks Links = SkillWikiLinks.From(
    [
        new Skill { Name = "Mimetism", Wiki = "https://wiki/mimetism" },
        new Skill { Name = "Climbing Plus" },
        new Skill { Name = "mimetism", Wiki = "https://wiki/second" }
    ]);

    [Theory]
    [InlineData("Mimetism (-6)", "https://wiki/mimetism")]
    [InlineData("MIMETISM", "https://wiki/mimetism")]
    [InlineData("Climbing Plus*", "https://infinitythewiki.com/Climbing_Plus?version=n4")]
    public void FindsASkillByItsNameWithoutBracketsOrMarks(string ability, string expected) =>
        Assert.Equal(expected, Links.For(ability));

    [Fact]
    public void AnUnknownAbilityHasNoLink() => Assert.Null(Links.For("Hacking Device"));

    [Fact]
    public void SplitsAnAbilityListIntoLinksInOrder()
    {
        Assert.Equal<AbilityLink>(
            [new("Mimetism (-3)", "https://wiki/mimetism"), new("Pistol", null)],
            Links.Links(" Mimetism (-3), ,Pistol "));
    }

    [Fact]
    public void NoAbilitiesHasNoLinks() => Assert.Empty(Links.Links(null));

    [Fact]
    public void ASkillLinkIsASkill()
    {
        Assert.True(new AbilityLink("Mimetism", "url").IsSkill);
        Assert.False(new AbilityLink("Pistol", null).IsSkill);
    }
}

public sealed class MetadataLookupsTests
{
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();

    [Fact]
    public void PrefersWeaponsNamedExactlyAsTheItem()
    {
        _metadata.SearchWeaponsByName("Combi Rifle").Returns(
        [
            new Weapon { Name = "Combi Rifle", Mode = "Shock" },
            new Weapon { Name = "Combi Rifle Plus" },
            new Weapon { Name = "combi rifle", Mode = "Normal" }
        ]);

        var weapons = _metadata.WeaponsFor("Combi Rifle (+1B)");

        Assert.Equal<string?>(["Shock", "Normal"], weapons.Select(weapon => weapon.Mode));
    }

    [Fact]
    public void FallsBackToEveryMatch()
    {
        _metadata.SearchWeaponsByName("Rifle").Returns([new Weapon { Name = "Combi Rifle" }, new Weapon { Name = "MULTI Rifle" }]);

        Assert.Equal(["Combi Rifle", "MULTI Rifle"], _metadata.WeaponsFor("Rifle").Select(weapon => weapon.Name));
    }
}
