using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Season;

public partial class MercsSeasonPage : AdaptiveContentPage
{
    public MercsSeasonPage(MercsSeasonPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        ApplyLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if WINDOWS
        // The window/Shell are fully live by the time the home page appears, so this is a
        // reliable point to wire up the Ctrl+Shift+Q/W/E/R resize shortcuts (idempotent).
        InfinityMercsApp.WinUI.App.EnsureWindowResizeShortcuts();
#endif

        // Resets the page to its main two-button state.
        await InitializeViewModelAsync();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void ApplyLayout()
    {
        if (IsExpandedOrWider)
        {
            // Split horizontally: brand on the left, action panel on the right.
            RootGrid.RowDefinitions = [new RowDefinition(GridLength.Star)];
            RootGrid.ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            ];

            Grid.SetRow(BrandPanel, 0);
            Grid.SetColumn(BrandPanel, 0);
            Grid.SetRow(ActionArea, 0);
            Grid.SetColumn(ActionArea, 1);

            ActionArea.Padding = new Thickness(24, 0);
            ActionArea.VerticalOptions = LayoutOptions.Center;
            ActionArea.HorizontalOptions = LayoutOptions.Center;
            ActionArea.MaximumWidthRequest = 420d;
        }
        else
        {
            // Stacked: brand above, actions anchored near the bottom.
            RootGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            RootGrid.RowDefinitions =
            [
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            ];

            Grid.SetRow(BrandPanel, 0);
            Grid.SetColumn(BrandPanel, 0);
            Grid.SetRow(ActionArea, 1);
            Grid.SetColumn(ActionArea, 0);

            ActionArea.Padding = new Thickness(24, 0, 24, 40);
            ActionArea.VerticalOptions = LayoutOptions.End;

            // Medium keeps the stacked layout but constrains the action column to a readable width.
            if (IsMedium)
            {
                ActionArea.HorizontalOptions = LayoutOptions.Center;
                ActionArea.MaximumWidthRequest = 520d;
            }
            else
            {
                ActionArea.HorizontalOptions = LayoutOptions.Fill;
                ActionArea.MaximumWidthRequest = double.PositiveInfinity;
            }
        }
    }
}
