using InfinityMercsApp.ViewModels.Season.Overview;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Overview;

public partial class SeasonPage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public SeasonViewModel ViewModel { get; }

    public SeasonPage(SeasonViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
