using InfinityMercsApp.ViewModels.Season.Bye;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Bye;

public partial class ByeRoundPage : ContentPageBase
{
    public ByeRoundPage(ByeRoundViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
