using InfinityMercsApp.ViewModels.Company;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Company;

public partial class LoadCompanyPage : AdaptiveContentPage
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public LoadCompanyViewModel ViewModel { get; }

    public LoadCompanyPage(LoadCompanyViewModel viewModel)
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
