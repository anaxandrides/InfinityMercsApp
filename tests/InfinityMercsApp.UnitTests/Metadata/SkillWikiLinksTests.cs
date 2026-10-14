using InfinityMercsApp.Domain.Models.Metadata;

namespace InfinityMercsApp.UnitTests.Metadata;

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
