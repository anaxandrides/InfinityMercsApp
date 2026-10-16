using InfinityMercsApp.ViewModels.Season.Bye;

namespace InfinityMercsApp.Views.Season;

public partial class ByeRoundPage : ContentPageBase
{
    public ByeRoundPage(ByeRoundViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
