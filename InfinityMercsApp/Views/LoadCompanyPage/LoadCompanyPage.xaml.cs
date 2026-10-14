using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

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
