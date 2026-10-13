using InfinityMercsApp.Domain.Lieutenants;

namespace InfinityMercsApp.UnitTests.Lieutenants;

public sealed class LieutenantRulesTests
{
    private static readonly string NewLine = Environment.NewLine;

    [Theory]
    [InlineData("Lieutenant", true, true)]
    [InlineData("LIEUTENANT (+1 Order)", true, true)]
    [InlineData("Lt", false, true)]
    [InlineData("Lt. Order", false, true)]
    [InlineData("Lightning", false, false)]
    [InlineData(null, false, false)]
    public void RecognisesLieutenantSkills(string? skill, bool mentions, bool isSkill)
    {
        Assert.Equal(mentions, LieutenantRules.Mentions(skill));
        Assert.Equal(isSkill, LieutenantRules.IsLieutenantSkill(skill));
    }

    [Theory]
    [InlineData("+1 Lt Order", true)]
    [InlineData("Lieutenant Order", true)]
    [InlineData("Courage", false)]
    public void MentionsLieutenantOrders(string line, bool expected) =>
        Assert.Equal(expected, LieutenantRules.MentionsLieutenantOrders(line));

    [Fact]
    public void AddSkillIfMissing_AddsOnlyWhenNoLieutenantSkillIsListed()
    {
        var withoutLt = new List<string> { "Courage" };
        LieutenantRules.AddSkillIfMissing(withoutLt);
        Assert.Equal(new[] { "Courage", "Lieutenant" }, withoutLt);

        var withAbbreviation = new List<string> { "Lt" };
        LieutenantRules.AddSkillIfMissing(withAbbreviation);
        Assert.Equal(new[] { "Lt" }, withAbbreviation);
    }

    [Fact]
    public void WithSkill_SortsDistinctAndAddsLieutenant() =>
        Assert.Equal(new[] { "Courage", "Lieutenant", "Sniper" }, LieutenantRules.WithSkill(["Sniper", "Courage", "sniper", " "]));

    [Fact]
    public void MoveLieutenantFirst_MovesTheFirstLieutenantSkill()
    {
        var skills = new List<string> { "Courage", "Lieutenant (+1 Order)", "Sniper" };
        LieutenantRules.MoveLieutenantFirst(skills);
        Assert.Equal(new[] { "Lieutenant (+1 Order)", "Courage", "Sniper" }, skills);
    }

    [Theory]
    [InlineData("Lieutenant (+1 Order)", "+1 Order")]
    [InlineData("Lieutenant", null)]
    [InlineData("Courage", null)]
    public void SkillDetail_ReturnsWhatTheSkillAdds(string skill, string? expected) =>
        Assert.Equal(expected, LieutenantRules.SkillDetail(skill));

    [Theory]
    [InlineData("Infinity Spec-Ops", true)]
    [InlineData("Lieutenant", true)]
    [InlineData("Courage", false)]
    public void IsCommonSpecOpsSkill(string skill, bool expected) =>
        Assert.Equal(expected, LieutenantRules.IsCommonSpecOpsSkill(skill));

    [Fact]
    public void HighlightRuns_SplitOutEveryLieutenant()
    {
        Assert.Equal(
            new[] { ("Lieutenant", true), (", ", false), ("LIEUTENANT", true), (" (+1)", false) },
            LieutenantRules.HighlightRuns("Lieutenant, LIEUTENANT (+1)"));
        Assert.Equal(new[] { ("Courage", false) }, LieutenantRules.HighlightRuns("Courage"));
        Assert.Empty(LieutenantRules.HighlightRuns(""));
    }

    [Theory]
    [InlineData("Courage", 0)]
    [InlineData("+1 Lt Order", 1)]
    [InlineData("+2 Lieutenant Orders", 2)]
    [InlineData("Lieutenant (+1 Lt Order)", 1)]
    public void CountBonusOrders_CountsStandaloneAndDetailedOrders(string skills, int expected) =>
        Assert.Equal(expected, LieutenantRules.CountBonusOrders(skills));

    [Fact]
    public void CountBonusOrders_AddsAcrossLines() =>
        Assert.Equal(3, LieutenantRules.CountBonusOrders($"+1 Lt Order{NewLine}Courage{NewLine}Lieutenant (+2 Orders)"));

    [Fact]
    public void NormalizeOrderEntries_RewritesOrdersAndDropsRepeats()
    {
        Assert.Equal($"Courage{NewLine}+1 Lt Order", LieutenantRules.NormalizeOrderEntries($"Courage{NewLine}+1 Regular Order{NewLine}courage"));
        Assert.Equal("Lieutenant (+1 Lt Order)", LieutenantRules.NormalizeOrderEntries($"Lieutenant (+1 Lt Order){NewLine}+1 Order"));
        Assert.Equal("-", LieutenantRules.NormalizeOrderEntries(" - "));
    }
}
