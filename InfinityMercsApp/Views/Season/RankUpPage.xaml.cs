using InfinityMercsApp.ViewModels.Season.RankUp;

namespace InfinityMercsApp.Views.Season;

public partial class RankUpPage : ContentPageBase
{
    public RankUpPage(RankUpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
