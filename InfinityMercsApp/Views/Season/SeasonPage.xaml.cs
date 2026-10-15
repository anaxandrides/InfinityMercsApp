using InfinityMercsApp.ViewModels.Season.Overview;

namespace InfinityMercsApp.Views.Season;

public partial class SeasonPage : ContentPageBase
{
    public SeasonPage(SeasonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
