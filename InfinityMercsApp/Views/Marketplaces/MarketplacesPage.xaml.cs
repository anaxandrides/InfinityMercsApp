using InfinityMercsApp.ViewModels.Marketplaces;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Marketplaces;

public partial class MarketplacesPage : AdaptiveContentPage
{
    public MarketplacesPage(MarketplacesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitializeViewModelAsync();
    }
}
