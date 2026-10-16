using InfinityMercsApp.ViewModels.Season.RankUp;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.RankUp;

public partial class RankUpPage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public RankUpViewModel ViewModel { get; }

    public RankUpPage(RankUpViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
