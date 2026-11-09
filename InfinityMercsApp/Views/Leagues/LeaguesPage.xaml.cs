using InfinityMercsApp.ViewModels.Leagues;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Leagues;

public partial class LeaguesPage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public LeaguesViewModel ViewModel { get; }

    public LeaguesPage(LeaguesViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
