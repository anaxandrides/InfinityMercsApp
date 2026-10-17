using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Presentation;
using InfinityMercsApp.Presentation.Units;

namespace InfinityMercsApp.UnitTests.Presentation.Units;

public sealed class UnitDetailTextTests
{
    [Fact]
    public void Summaries()
    {
        Assert.Equal("Equipment: a, B", UnitDetailText.NamedSummary("Equipment", ["B", "a", "b", " "]));
        Assert.Equal("Skills: -", UnitDetailText.NamedSummary("Skills", []));
        Assert.Equal($"x{Environment.NewLine}y", UnitDetailText.JoinOrDash(["x", "y", "X"]));
        Assert.Equal("-", UnitDetailText.JoinOrDash([""]));
    }

    [Theory]
    [InlineData("CCW", true)]
    [InlineData("AP CCW", true)]
    [InlineData("Breaker Pistol", true)]
    [InlineData("Combi Rifle", false)]
    [InlineData("Spitfire", false)]
    public void MeleeWeapons(string weapon, bool melee) => Assert.Equal(melee, UnitDetailText.IsMeleeWeaponName(weapon));

    [Fact]
    public void ConfigurationSkillsKeepWhatALieutenantSkillAdds()
    {
        var entries = UnitDetailText.ConfigurationSkillEntries(
            [new(1, "Sniper"), new(2, "Lieutenant (+1 Order)"), new(3, "Lieutenant"), new(4, "sniper"), new(5, " ")]);

        Assert.Equal([new NamedEntry(0, "+1 Order"), new NamedEntry(1, "Sniper")], entries);
    }

    [Fact]
    public void LinkedLinesMergeRepeatsAndKeepTheFirstLinkFound()
    {
        var links = new Dictionary<int, string> { [2] = "wiki/mimetism" };

        var lines = UnitDetailText.LinkedLines([new(1, "Mimetism"), new(2, "mimetism"), new(3, "Hacker"), new(4, " ")], links);

        Assert.Equal([new LinkedText("Mimetism", "wiki/mimetism"), new LinkedText("Hacker", null)], lines);
    }

    [Fact]
    public void LinksMatchTheWholeNameOrTheNameBeforeItsDetail()
    {
        var names = new Dictionary<int, string> { [1] = "Mimetism", [2] = "Hacker" };
        var links = new Dictionary<int, string> { [1] = "wiki/mimetism" };

        Assert.Equal("wiki/mimetism", UnitDetailText.LinkForDisplayName("mimetism (-6)", names, links));
        Assert.Null(UnitDetailText.LinkForDisplayName("Hacker", names, links));
    }

    [Fact]
    public void MergingDropsAStandaloneLtOrderTheLieutenantSkillAlreadyGives() =>
        Assert.Equal(
            "Courage, Lieutenant (+1 Lt Order)",
            UnitDetailText.MergeSummaryAndUnique("Special Skills: Courage, +1 Lt Order", "Lieutenant (+1 Order)"));

    [Fact]
    public void MergingNothingIsADash() => Assert.Equal("-", UnitDetailText.MergeSummaryAndUnique("Equipment: -", "-"));

    [Fact]
    public void SubtitleFallsBackToIds() =>
        Assert.Equal("Line Infantry - 7", UnitDetailText.UnitSubtitle(1, 7, new Dictionary<int, string> { [1] = "Line Infantry" }, new Dictionary<int, string>()));
}
