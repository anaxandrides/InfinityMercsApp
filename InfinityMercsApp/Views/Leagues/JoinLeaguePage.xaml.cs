using InfinityMercsApp.ViewModels.Leagues;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Leagues;

public partial class JoinLeaguePage : ContentPageBase
{
    public JoinLeaguePage(JoinLeagueViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
