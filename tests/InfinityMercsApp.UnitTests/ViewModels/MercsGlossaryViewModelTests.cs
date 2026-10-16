using InfinityMercsApp.Services.Glossary;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class MercsGlossaryViewModelTests
{
    private const string Index = """{"sections":[{"header":"Rules","entries":[{"title":"Orders","markdownFile":"orders.md"},{"title":"Empty"}]}]}""";

    private readonly IAppPackageFiles _files = Substitute.For<IAppPackageFiles>();

    private async Task<MercsGlossaryViewModel> OpenAsync()
    {
        _files.ReadTextAsync(MercsGlossary.IndexFile).Returns(Index);
        _files.ReadTextAsync("glossary/orders.md").Returns("# Orders");
        var viewModel = new MercsGlossaryViewModel(Substitute.For<INavigationService>(), _files);
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
        var viewModel = new MercsGlossaryViewModel(Substitute.For<INavigationService>(), _files);
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
        var viewModel = new MercsGlossaryViewModel(Substitute.For<INavigationService>(), _files);

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        Assert.Equal("Load Error", viewModel.SelectedTitle);
        Assert.Contains("Failed to load glossary data: gone", viewModel.SelectedContentHtml);
    }

    [Theory]
    [InlineData(AdaptiveLayoutMode.Compact, 17)]
    [InlineData(AdaptiveLayoutMode.Medium, 14)]
    [InlineData(AdaptiveLayoutMode.Expanded, 15)]
    [InlineData(AdaptiveLayoutMode.Wide, 16)]
    public async Task ScalesTheIndexTextToTheWidth(AdaptiveLayoutMode mode, double fontSize)
    {
        var viewModel = await OpenAsync();
        viewModel.ApplyLayoutCommand.Execute(mode == AdaptiveLayoutMode.Wide ? AdaptiveLayoutMode.Compact : AdaptiveLayoutMode.Wide);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.ApplyLayoutCommand.Execute(mode);

        Assert.Equal(fontSize, viewModel.ListFontSize);
        Assert.Contains(nameof(MercsGlossaryViewModel.ListFontSize), changed);
    }

    [Fact]
    public async Task OnAPhoneTheRuleTextReplacesTheIndex()
    {
        var viewModel = await OpenAsync();
        Assert.True(viewModel.IsIndexPaneVisible);

        viewModel.IsShowingDetail = true;

        Assert.False(viewModel.IsIndexPaneVisible);
        Assert.True(viewModel.IsDetailPaneVisible);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Medium);
        Assert.True(viewModel.IsIndexPaneVisible);
        Assert.Equal("300,*", viewModel.Layout.Columns);
    }
}
