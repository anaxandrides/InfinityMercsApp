using InfinityMercsApp.Services.Wiki;

namespace InfinityMercsApp.UnitTests.Services.Wiki;

/// <summary>A wiki page turns into the blocks a rule popup shows: the rule's box when it has one, else the page's text.</summary>
public sealed class WikiDescriptionServiceTests
{
    private const string Url = "https://wiki.example/Camouflage";

    private static async Task<IReadOnlyList<WikiContentBlock>> Read(string html, string? section = null) =>
        await new WikiDescriptionService(new HttpClient(new PageHandler(html))).FetchContentAsync(Url, section);

    private static IReadOnlyList<string> Texts(IReadOnlyList<WikiContentBlock> blocks) => blocks.Select(block => block.Text).ToList();

    [Fact]
    public async Task FetchContentAsync_RuleBox_ReadsOnlyTheBox()
    {
        var blocks = await Read("""<p>Intro</p><div class="cssbox cssbox-greenyellow"><h3>Camo</h3><p>Hides &amp; waits.</p></div><p>After</p>""");

        Assert.Equal(["Camo", "Hides & waits."], Texts(blocks));
        Assert.Equal([WikiBlockType.SectionHeader, WikiBlockType.Paragraph], blocks.Select(block => block.Type));
    }

    [Fact]
    public async Task FetchContentAsync_BlackAndPlainBoxes_ReadsBothBlackFirst()
    {
        var blocks = await Read("""<div class='cssbox'><p>Plain</p></div><div class="x cssbox-black"><p>Black</p></div>""");

        Assert.Equal(["Black", "Plain"], Texts(blocks));
    }

    [Fact]
    public async Task FetchContentAsync_NestedDivsInBox_ReadsTheWholeBox()
    {
        var blocks = await Read("""<div class="cssbox"><div><p>One</p></div><p>Two</p></div><p>Outside</p>""");

        Assert.Equal(["One", "Two"], Texts(blocks));
    }

    [Theory]
    [InlineData("""<div class="cssbox"/><h2>Title</h2><p>Text</p>""")]
    [InlineData("""<h2>Title</h2><p>Text</p><div class="cssbox"><p>never closed""")]
    public async Task FetchContentAsync_BoxUnusable_ReadsThePageWithoutItsHeading(string html)
    {
        var blocks = await Read(html);

        Assert.Equal("Text", blocks[0].Text);
    }

    [Fact]
    public async Task FetchContentAsync_NoBox_LeavesOutCollapsedOldRules()
    {
        var blocks = await Read("""<h2>Camo</h2><p>Current.</p><div class="mw-collapsible mw-collapsed"><div><p>Old.</p></div></div><p>More.</p>""");

        Assert.Equal(["Current.", "More."], Texts(blocks));
    }

    [Fact]
    public async Task FetchContentAsync_UnclosedCollapsedElement_IsKept()
    {
        var blocks = await Read("""<p>Current.</p><div class="mw-collapsed"><p>Old.</p>""");

        Assert.Equal(["Current.", "Old."], Texts(blocks));
    }

    [Fact]
    public async Task FetchContentAsync_Section_ReadsOnlyThatSection()
    {
        const string html = """
            <h2>Overview</h2><p>Skip.</p>
            <h2 id="x"><span>Requirements</span></h2><p>Need it.</p><h3>Detail</h3><p>Still in.</p>
            <h2>Effects</h2><p>Skip too.</p>
            """;

        var blocks = await Read(html, "requirements");

        Assert.Equal(["Need it.", "Detail", "Still in."], Texts(blocks));
    }

    [Fact]
    public async Task FetchContentAsync_LastSection_ReadsToTheEnd() =>
        Assert.Equal(["End."], Texts(await Read("<h2>First</h2><p>No.</p><h2>Last</h2><p>End.</p>", "Last")));

    [Theory]
    [InlineData("<h2>Other</h2><p>Text.</p>")]
    [InlineData("<h2>Missing never closes<p>Text.</p>")]
    public async Task FetchContentAsync_SectionNotFound_ReadsTheWholePage(string html) =>
        Assert.Contains("Text.", Texts(await Read(html, "Missing")));

    [Fact]
    public async Task FetchContentAsync_NestedLists_IndentAndMarkBold()
    {
        var blocks = await Read("<p>Intro</p><ul><li><b>Bold</b> item<ul><li>Nested</li></ul></li><li>Plain<li>Unclosed</ul>");

        Assert.Equal(["Intro", "Bold item", "Nested", "Plain", "Unclosed"], Texts(blocks));
        var bullets = blocks.Skip(1).ToList();
        Assert.Equal([true, false, false, false], bullets.Select(block => block.Bold));
        Assert.Equal([0, 1, 0, 0], bullets.Select(block => block.IndentLevel));
    }

    [Fact]
    public async Task FetchContentAsync_DefinitionList_TermsBoldDefinitionsIndented()
    {
        var blocks = await Read("<p>Intro</p><dl><dt>Term</dt><dd>Meaning</dd><dt>Open term<dd>Open meaning</dl>");

        Assert.Equal(["Intro", "Term", "Meaning", "Open term", "Open meaning"], Texts(blocks));
        Assert.Equal([true, false, true, false], blocks.Skip(1).Select(block => block.Bold));
        Assert.Equal([0, 1, 0, 1], blocks.Skip(1).Select(block => block.IndentLevel));
    }

    [Fact]
    public async Task FetchContentAsync_BreaksAndSpacing_AreNormalised()
    {
        var blocks = await Read("<p>Intro</p><p>One<br/>two\n\n  three<!-- note --></p><p>   </p><h3>Unclosed heading");

        Assert.Equal(["Intro", "One two three", "Unclosed heading"], Texts(blocks));
    }

    [Fact]
    public async Task FetchContentAsync_UnclosedParagraphs_EachBecomeABlock() =>
        Assert.Equal(["Intro", "One", "Two"], Texts(await Read("<p>Intro<p>One<p>Two")));

    [Fact]
    public async Task FetchContentAsync_TagNeverEnds_StopsThere() =>
        Assert.Equal(["Intro", "Text"], Texts(await Read("<p>Intro</p><p>Text<b")));

    [Fact]
    public async Task FetchContentAsync_SamePageAndSection_IsFetchedOnce()
    {
        var handler = new PageHandler("<p>Intro</p><p>Text</p>");
        var service = new WikiDescriptionService(new HttpClient(handler));

        await service.FetchContentAsync(Url);
        var again = await service.FetchContentAsync(Url.ToUpperInvariant());
        await service.FetchContentAsync(Url, "Text");

        Assert.Equal(["Intro", "Text"], Texts(again));
        Assert.Equal(2, handler.Requests);
    }

    [Fact]
    public async Task FetchContentAsync_WikiDown_IsEmptyAndTriesAgainNextTime()
    {
        var handler = new PageHandler(null);
        var service = new WikiDescriptionService(new HttpClient(handler));

        Assert.Empty(await service.FetchContentAsync(Url));
        Assert.Empty(await service.FetchContentAsync(Url));
        Assert.Equal(2, handler.Requests);
    }
}
