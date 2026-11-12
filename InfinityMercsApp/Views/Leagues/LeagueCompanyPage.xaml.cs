using InfinityMercsApp.ViewModels.Leagues;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Leagues;

public partial class LeagueCompanyPage : ContentPageBase
{
    public LeagueCompanyPage(LeagueCompanyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
