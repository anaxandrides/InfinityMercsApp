using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Company.Viewer;

public partial class CompanyViewerPage : ContentPageBase
{
    public CompanyViewerPage(CompanyViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
