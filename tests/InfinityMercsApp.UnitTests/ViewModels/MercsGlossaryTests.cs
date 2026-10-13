using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels;

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

public sealed class MercsGlossaryViewModelTests
{
    private const string Index = """{"sections":[{"header":"Rules","entries":[{"title":"Orders","markdownFile":"orders.md"},{"title":"Empty"}]}]}""";

    private readonly IAppPackageFiles _files = Substitute.For<IAppPackageFiles>();

    private async Task<MercsGlossaryViewModel> OpenAsync()
    {
        _files.ReadTextAsync(MercsGlossary.IndexFile).Returns(Index);
        _files.ReadTextAsync("glossary/orders.md").Returns("# Orders");
        var viewModel = new MercsGlossaryViewModel(new FakeNavigationService(), _files);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task OpensOnTheFirstRuleWithoutLeavingTheIndex()
    {
        var viewModel = await OpenAsync();

        Assert.Equal(3, viewModel.GlossaryItems.Count);
        Assert.Equal("Orders", viewModel.SelectedTitle);
        Assert.Contains("<h1", viewModel.SelectedContentHtml);
        Assert.Equal(viewModel.SelectedContentHtml, viewModel.ContentSource.Html);
        Assert.False(viewModel.IsShowingDetail);
    }

    [Fact]
    public async Task ChoosingARuleShowsItAndARuleWithoutAFileSaysSo()
    {
        var viewModel = await OpenAsync();

        viewModel.SelectedItem = viewModel.GlossaryItems[2];

        Assert.True(viewModel.IsShowingDetail);
        Assert.Equal("Empty", viewModel.SelectedTitle);
        Assert.Contains("No details available", viewModel.SelectedContentHtml);

        viewModel.CloseDetailCommand.Execute(null);
        Assert.False(viewModel.IsShowingDetail);
    }

    [Fact]
    public async Task HeadersCannotBeSelected()
    {
        var viewModel = await OpenAsync();
        var current = viewModel.SelectedItem;

        viewModel.SelectedItem = viewModel.GlossaryItems[0];

        Assert.Same(current, viewModel.SelectedItem);
        Assert.False(viewModel.IsShowingDetail);
    }

    [Fact]
    public async Task ASlowLoadDoesNotReplaceALaterChoice()
    {
        var slow = new TaskCompletionSource<string>();
        _files.ReadTextAsync("glossary/slow.md").Returns(slow.Task);
        _files.ReadTextAsync(MercsGlossary.IndexFile).Returns(
            """{"entries":[{"title":"Slow","markdownFile":"slow.md"},{"title":"Fast"}]}""");
        var viewModel = new MercsGlossaryViewModel(new FakeNavigationService(), _files);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        viewModel.SelectedItem = viewModel.GlossaryItems[1];
        slow.SetResult("# Too late");

        Assert.Equal("Fast", viewModel.SelectedTitle);
        Assert.DoesNotContain("Too late", viewModel.SelectedContentHtml);
    }

    [Fact]
    public async Task AMissingIndexShowsTheError()
    {
        _files.ReadTextAsync(MercsGlossary.IndexFile).ThrowsAsync(new FileNotFoundException("gone"));
        var viewModel = new MercsGlossaryViewModel(new FakeNavigationService(), _files);

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        Assert.Equal("Load Error", viewModel.SelectedTitle);
        Assert.Contains("Failed to load glossary data: gone", viewModel.SelectedContentHtml);
    }
}
