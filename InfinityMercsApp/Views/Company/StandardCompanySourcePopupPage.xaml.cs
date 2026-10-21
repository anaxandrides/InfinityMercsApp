using InfinityMercsApp.ViewModels.Company;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Company;

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
