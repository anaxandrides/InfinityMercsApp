using InfinityMercsApp.Services.Glossary;

namespace InfinityMercsApp.UnitTests.Services.Glossary;

public sealed class MercsGlossaryTests
{
    [Fact]
    public void ParseIndex_SectionsGiveHeadersThenEntries()
    {
        var items = MercsGlossary.ParseIndex("""
            {"sections":[
              {"header":" Basics ","entries":[{"title":"Orders","markdownFile":" orders.md ","hasDelta":true},{"title":" "}]},
              {"title":"Combat","entries":[{"title":"Cover"}]}
            ]}
            """);

        Assert.Equal(
            [
                new GlossaryEntry("Basics", IsHeader: true),
                new GlossaryEntry("Orders", "orders.md", HasDelta: true),
                new GlossaryEntry("Combat", IsHeader: true),
                new GlossaryEntry("Cover")
            ],
            items);
        Assert.True(items[1].ShowDeltaIcon);
        Assert.False(items[0].ShowDeltaIcon);
    }

    [Fact]
    public void ParseIndex_FlatEntriesStartAHeaderWhenTheSectionChanges()
    {
        var items = MercsGlossary.ParseIndex("""
            {"entries":[
              {"title":"A","sectionHeader":"One"},
              {"title":"B","header":"One"},
              {"title":"C","sectionHeader":"Two"}
            ]}
            """);

        Assert.Equal(["One", "A", "B", "Two", "C"], items.Select(i => i.Title));
    }

    [Fact]
    public void Pages_RenderMarkdownAndEncodePlainText()
    {
        Assert.Contains("<strong>bold</strong>", MercsGlossary.MarkdownPage("**bold**"));
        Assert.Contains("<p>a &lt;b&gt;<br />c</p>", MercsGlossary.TextPage("a <b>\nc"));
    }
}
