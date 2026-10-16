using InfinityMercsApp.ViewModels.Season;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Season;

public partial class LoadSeasonPage : AdaptiveContentPage
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public LoadSeasonViewModel ViewModel { get; }

    public LoadSeasonPage(LoadSeasonViewModel viewModel)
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
