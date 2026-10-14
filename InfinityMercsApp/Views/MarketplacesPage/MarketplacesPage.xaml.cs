using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

/// <summary>Lays out <see cref="MarketplacesViewModel"/> for the current width: stacked on phones, side rails and a cart on wider screens.</summary>
public partial class MarketplacesPage : AdaptiveContentPage
{
    private readonly MarketplacesViewModel _viewModel;

    public MarketplacesPage(MarketplacesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
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
        // The buying workflow (and its cart summary) is offered only on the widest screens.
        _viewModel.ShowQuantityControls = IsWide;

        if (IsCompact || IsMedium)
        {
            ContentGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            ContentGrid.RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star)];
            ContentGrid.ColumnSpacing = 0;
            ContentGrid.RowSpacing = 12;
            Place(StoreInfoBorder, 0, 0);
            Place(SectionsScroll, 1, 0);
        }
        else
        {
            // Expanded has the store rail and sections; wide adds the cart rail on the right.
            ContentGrid.ColumnDefinitions = IsWide
                ? [new ColumnDefinition(new GridLength(280)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(320))]
                : [new ColumnDefinition(new GridLength(280)), new ColumnDefinition(GridLength.Star)];
            ContentGrid.RowDefinitions = [new RowDefinition(GridLength.Star)];
            ContentGrid.ColumnSpacing = 12;
            ContentGrid.RowSpacing = 0;
            Place(StoreInfoBorder, 0, 0);
            Place(SectionsScroll, 0, 1);
            if (IsWide)
            {
                Place(CartBorder, 0, 2);
            }
        }

        ApplySectionsColumns(twoColumn: !IsCompact);

        // Placeholder always covers the whole content area.
        Place(PlaceholderLabel, 0, 0, ContentGrid.RowDefinitions.Count, ContentGrid.ColumnDefinitions.Count);
    }

    // Two columns put the items on the left, with troop types above augments on the right.
    private void ApplySectionsColumns(bool twoColumn)
    {
        if (twoColumn)
        {
            SectionsHost.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)];
            SectionsHost.RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
            Place(ItemsBorder, 0, 0, rowSpan: 2);
            Place(TroopTypesBorder, 0, 1);
            Place(AugmentsBorder, 1, 1);
        }
        else
        {
            SectionsHost.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            SectionsHost.RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
            Place(ItemsBorder, 0, 0);
            Place(TroopTypesBorder, 1, 0);
            Place(AugmentsBorder, 2, 0);
        }
    }

    private static void Place(View view, int row, int column, int rowSpan = 1, int columnSpan = 1)
    {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, column);
        Grid.SetRowSpan(view, rowSpan);
        Grid.SetColumnSpan(view, columnSpan);
    }
}
