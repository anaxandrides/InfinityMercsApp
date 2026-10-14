using InfinityMercsApp.ViewModels.Season;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Season;

public partial class LoadSeasonPage : AdaptiveContentPage
{
    public LoadSeasonPage(LoadSeasonViewModel viewModel)
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
