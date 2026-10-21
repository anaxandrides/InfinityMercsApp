using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Company.Selection;

/// <summary>Builds a new company of whichever kind its ViewModel is.</summary>
public partial class CompanySelectionPage : AdaptiveContentPage
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public CompanySelectionViewModel ViewModel { get; }

    public CompanySelectionPage(CompanySelectionViewModel viewModel)
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
