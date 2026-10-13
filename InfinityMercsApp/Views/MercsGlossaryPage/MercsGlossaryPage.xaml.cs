using System.ComponentModel;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

/// <summary>
/// MERCS rules glossary. <see cref="MercsGlossaryViewModel"/> holds the index and selected rule; the
/// code-behind only arranges the panes and sizes the index text for the layout.
/// </summary>
public partial class MercsGlossaryPage : AdaptiveContentPage
{
    private readonly MercsGlossaryViewModel _viewModel;

    public MercsGlossaryPage(MercsGlossaryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyLayout();
    }

    /// <summary>
    /// Rules-index list text size, scaled to the available width so long titles stay readable and
    /// fit inside the index box. Compact has the full screen width; the medium+ rail is narrow.
    /// </summary>
    public double ListFontSize => LayoutMode switch
    {
        AdaptiveLayoutMode.Compact => 17d,
        AdaptiveLayoutMode.Medium => 14d,
        AdaptiveLayoutMode.Expanded => 15d,
        _ => 16d
    };

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeViewModelAsync();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MercsGlossaryViewModel.IsShowingDetail))
        {
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        OnPropertyChanged(nameof(ListFontSize));

        if (IsCompact)
        {
            RootGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            RootGrid.ColumnSpacing = 0;
            Grid.SetColumn(IndexPane, 0);
            Grid.SetColumn(DetailPane, 0);
            IndexPane.IsVisible = !_viewModel.IsShowingDetail;
            DetailPane.IsVisible = _viewModel.IsShowingDetail;
            DetailBackButton.IsVisible = true;
        }
        else
        {
            var indexWidth = LayoutMode switch
            {
                AdaptiveLayoutMode.Medium => 300d,
                AdaptiveLayoutMode.Expanded => 320d,
                _ => 340d
            };

            RootGrid.ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(indexWidth)),
                new ColumnDefinition(GridLength.Star)
            ];
            RootGrid.ColumnSpacing = 16;
            Grid.SetColumn(IndexPane, 0);
            Grid.SetColumn(DetailPane, 1);
            IndexPane.IsVisible = true;
            DetailPane.IsVisible = true;
            DetailBackButton.IsVisible = false;
        }

        // Keep rules text at a comfortable reading width on the largest screens. The WebView must
        // stay Fill — it has no intrinsic content width, so centering collapses it to zero and the
        // text disappears. Cap the width via MaximumWidthRequest instead.
        GlossaryWebView.HorizontalOptions = LayoutOptions.Fill;
        GlossaryWebView.MaximumWidthRequest = IsWide ? 860d : double.PositiveInfinity;
    }
}
