using InfinityMercsApp.ViewModels.Marketplaces;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Marketplaces;

public partial class MarketplacesPage : AdaptiveContentPage
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public MarketplacesViewModel ViewModel { get; }

    public MarketplacesPage(MarketplacesViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeViewModelAsync();
    }
}
