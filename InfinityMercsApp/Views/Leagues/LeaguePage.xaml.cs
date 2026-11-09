using InfinityMercsApp.ViewModels.Leagues;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Leagues;

public partial class LeaguePage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public LeagueViewModel ViewModel { get; }

    public LeaguePage(LeagueViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
