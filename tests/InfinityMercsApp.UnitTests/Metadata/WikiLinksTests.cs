using InfinityMercsApp.Domain.Models.Metadata.Wiki;

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
