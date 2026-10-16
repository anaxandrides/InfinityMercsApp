using InfinityMercsApp.ViewModels.Armory;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Armory;

public partial class ArmoryPage : AdaptiveContentPage
{
    public ArmoryPage(ArmoryViewModel viewModel)
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
