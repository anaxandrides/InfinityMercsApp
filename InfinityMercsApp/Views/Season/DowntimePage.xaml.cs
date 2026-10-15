using InfinityMercsApp.ViewModels.Season.Downtime;

namespace InfinityMercsApp.Views.Season;

public partial class DowntimePage : ContentPageBase
{
    public DowntimePage(DowntimeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
