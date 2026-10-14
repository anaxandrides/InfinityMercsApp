using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

public partial class LoadCompanyPage : AdaptiveContentPage
{
    public LoadCompanyPage(LoadCompanyViewModel viewModel)
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
        // largest screens reflow saved records into a two-column grid.
        RootGrid.MaximumWidthRequest = IsCompact ? double.PositiveInfinity : (IsWide ? 1000d : 640d);
        RootGrid.HorizontalOptions = IsCompact ? LayoutOptions.Fill : LayoutOptions.Center;

        RecordsCollection.ItemsLayout = IsWide
            ? new GridItemsLayout(2, ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing = 8 }
            : new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
    }
}
