using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Season;

public partial class LoadSeasonPage : AdaptiveContentPage
{
    public LoadSeasonPage(LoadSeasonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        ApplyLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeViewModelAsync();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void ApplyLayout()
    {
        // Compact fills the screen; wider widths center the list at a readable width, and the
        // largest screens reflow saved seasons into a two-column grid.
        RootGrid.MaximumWidthRequest = IsCompact ? double.PositiveInfinity : (IsWide ? 1000d : 640d);
        RootGrid.HorizontalOptions = IsCompact ? LayoutOptions.Fill : LayoutOptions.Center;

        SeasonsCollection.ItemsLayout = IsWide
            ? new GridItemsLayout(2, ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing = 8 }
            : new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
    }
}
