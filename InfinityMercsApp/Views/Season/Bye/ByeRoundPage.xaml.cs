using InfinityMercsApp.ViewModels.Season.Bye;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Bye;

public partial class ByeRoundPage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public ByeRoundViewModel ViewModel { get; }

    public ByeRoundPage(ByeRoundViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
