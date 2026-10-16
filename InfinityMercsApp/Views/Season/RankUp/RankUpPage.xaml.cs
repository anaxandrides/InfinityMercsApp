using InfinityMercsApp.ViewModels.Season.RankUp;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.RankUp;

public partial class RankUpPage : ContentPageBase
{
    public RankUpPage(RankUpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
