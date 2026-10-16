using InfinityMercsApp.ViewModels.Company;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Company;

public partial class LoadCompanyPage : AdaptiveContentPage
{
    public LoadCompanyPage(LoadCompanyViewModel viewModel)
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
