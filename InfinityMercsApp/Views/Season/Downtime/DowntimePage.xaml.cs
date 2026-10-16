using InfinityMercsApp.ViewModels.Season.Downtime;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Downtime;

public partial class DowntimePage : ContentPageBase
{
    public DowntimePage(DowntimeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
