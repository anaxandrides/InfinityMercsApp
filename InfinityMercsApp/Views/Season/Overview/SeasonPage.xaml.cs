using InfinityMercsApp.ViewModels.Season.Overview;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Overview;

public partial class SeasonPage : ContentPageBase
{
    public SeasonPage(SeasonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
