using System.ComponentModel;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

/// <summary>
/// Weapon reference. <see cref="ArmoryViewModel"/> holds the list and selection; the code-behind only
/// switches between the side-by-side panes and, on phones, showing the list or the detail.
/// </summary>
public partial class ArmoryPage : AdaptiveContentPage
{
    private readonly ArmoryViewModel _viewModel;

    public ArmoryPage(ArmoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeViewModelAsync();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ArmoryViewModel.IsShowingDetail))
        {
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        if (IsCompact)
        {
            RootGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            RootGrid.ColumnSpacing = 0;
            Grid.SetColumn(ListPane, 0);
            Grid.SetColumn(DetailPane, 0);
            ListPane.IsVisible = !_viewModel.IsShowingDetail;
            DetailPane.IsVisible = _viewModel.IsShowingDetail;
            DetailBackButton.IsVisible = true;
        }
        else
        {
            var listWidth = LayoutMode switch
            {
                AdaptiveLayoutMode.Medium => 320d,
                AdaptiveLayoutMode.Expanded => 340d,
                _ => 360d
            };

            RootGrid.ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(listWidth)),
                new ColumnDefinition(GridLength.Star)
            ];
            RootGrid.ColumnSpacing = 16;
            Grid.SetColumn(ListPane, 0);
            Grid.SetColumn(DetailPane, 1);
            ListPane.IsVisible = true;
            DetailPane.IsVisible = true;
            DetailBackButton.IsVisible = false;
        }

        // Keep weapon cards at a comfortable reading width on the largest screens. Stay Fill and cap
        // via MaximumWidthRequest; centering can collapse a stack whose children have no fixed width.
        DetailWeaponList.HorizontalOptions = LayoutOptions.Fill;
        DetailWeaponList.MaximumWidthRequest = IsWide ? 820d : double.PositiveInfinity;
    }
}
