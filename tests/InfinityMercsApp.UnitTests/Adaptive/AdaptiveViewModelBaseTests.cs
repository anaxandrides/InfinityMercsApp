using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Adaptive;

public sealed class AdaptiveViewModelBaseTests
{
    private sealed class Sample() : AdaptiveViewModelBase<string>(Substitute.For<INavigationService>(), mode => $"layout {mode}")
    {
        public int Applied { get; private set; }

        protected override void OnLayoutApplied() => Applied++;
    }

    [Fact]
    public void StartsCompact()
    {
        var viewModel = new Sample();

        Assert.Equal("layout Compact", viewModel.Layout);
        Assert.True(viewModel.IsCompact);
        Assert.Equal(0, viewModel.Applied);
    }

    [Fact]
    public void AppliesTheLayoutForEachMode()
    {
        var viewModel = new Sample();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Wide);

        Assert.Equal("layout Wide", viewModel.Layout);
        Assert.Equal(AdaptiveLayoutMode.Wide, viewModel.LayoutMode);
        Assert.True(viewModel.IsWide);
        Assert.False(viewModel.IsCompact);
        Assert.Equal(1, viewModel.Applied);
        Assert.Contains("Layout", changed);
        Assert.Contains("IsCompact", changed);
    }
}
