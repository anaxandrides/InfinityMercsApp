using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

public partial class MercsGlossaryPage : AdaptiveContentPage
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public MercsGlossaryViewModel ViewModel { get; }

    public MercsGlossaryPage(MercsGlossaryViewModel viewModel)
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
