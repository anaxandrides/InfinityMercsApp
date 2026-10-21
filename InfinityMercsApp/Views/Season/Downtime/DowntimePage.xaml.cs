using InfinityMercsApp.ViewModels.Season.Downtime;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Downtime;

public partial class DowntimePage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public DowntimeViewModel ViewModel { get; }

    public DowntimePage(DowntimeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
