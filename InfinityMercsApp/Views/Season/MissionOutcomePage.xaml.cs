using InfinityMercsApp.ViewModels.Season;

namespace InfinityMercsApp.Views.Season;

public partial class MissionOutcomePage : ContentPageBase
{
    public MissionOutcomePage(MissionOutcomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
