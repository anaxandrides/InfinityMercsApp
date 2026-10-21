using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Company.Viewer;

public partial class CompanyViewerPage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public CompanyViewerViewModel ViewModel { get; }

    public CompanyViewerPage(CompanyViewerViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
