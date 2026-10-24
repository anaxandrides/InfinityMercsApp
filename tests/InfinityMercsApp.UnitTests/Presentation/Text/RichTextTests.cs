using InfinityMercsApp.Presentation.Text;
using InfinityMercsApp.Presentation.Units;

namespace InfinityMercsApp.UnitTests.Presentation.Text;

public sealed class RichTextTests
{
    [Fact]
    public void Equals_SameRuns_IsEqual() =>
        Assert.Equal(new RichText([new RichTextRun("a"), new RichTextRun("b")]), new RichText([new RichTextRun("a"), new RichTextRun("b")]));

    [Fact]
    public void PlainText_Runs_JoinsTheirText() =>
        Assert.Equal("Skills: Mimetism", UnitDetailFormatting.NamedSummary("Skills", ["Mimetism"], null, null, UnitDetailFormatting.SkillsColor).PlainText);

    [Fact]
    public void Name_Lieutenant_HighlightsTheWord()
    {
        var name = UnitDetailFormatting.Name("Fusilier Lieutenant");

        Assert.Contains(name.Runs, run => run.Text == "Lieutenant" && run.Color == UnitDetailFormatting.LieutenantColor);
    }
}
