using InfinityMercsApp.ViewModels.Season;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season;

public partial class MissionOutcomePage : ContentPageBase
{
    public MissionOutcomePage(MissionOutcomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
