using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.UnitTests.Company;

public sealed class SkillDisplayTests
{
    private static string Lines(params IEnumerable<string> lines) => string.Join(Environment.NewLine, lines);

    [Fact]
    public void RepeatsAreDroppedIgnoringCaseSpacingAndArgumentOrder() =>
        Assert.Equal(
            Lines("Mimetism (-6)", "Climbing Plus", "Hacker (Killer, Evo)"),
            SkillDisplay.Normalize(Lines("Mimetism (-6)", "mimetism  (-6)", "Climbing Plus", "Hacker (Killer, Evo)", "Hacker (Evo, Killer)")));

    [Fact]
    public void DodgesMergeIntoOneLineAtTheEnd() =>
        Assert.Equal(
            Lines("Courage", "Dodge (+1\")", "Dodge (PH=14)"),
            SkillDisplay.Normalize(Lines("Dodge", "Dodge (+1\")", "Courage", "Dodge (PH=14)", "dodge (+1\")")));

    [Fact]
    public void APlainDodgeStaysWhenItIsTheOnlyOne() =>
        Assert.Equal(Lines("Courage", "Dodge"), SkillDisplay.Normalize(Lines("Dodge", "Courage")));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-")]
    public void NoSkillsReadsAsADash(string? skills) => Assert.Equal("-", SkillDisplay.Normalize(skills));

    [Theory]
    [InlineData("  Hacker  (Evo,Killer, evo) ", "Hacker (Evo, Killer)")]
    [InlineData("Courage", "Courage")]
    [InlineData("Specialist ()", "Specialist")]
    [InlineData("  ", "")]
    public void CanonicalLineSortsAndDedupesArguments(string line, string expected) =>
        Assert.Equal(expected, SkillDisplay.CanonicalLine(line));
}
