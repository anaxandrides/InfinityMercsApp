using InfinityMercsApp.ViewModels.Company;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

public partial class StandardCompanySourcePopupPage : AdaptiveContentPage
{
    public StandardCompanySourcePopupPage(StandardCompanySourcePopupPageViewModel viewModel)
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
