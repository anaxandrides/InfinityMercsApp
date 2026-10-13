using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.Season;

public partial class MissionOutcomePage : ContentPageBase
{
    public MissionOutcomePage(MissionOutcomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
