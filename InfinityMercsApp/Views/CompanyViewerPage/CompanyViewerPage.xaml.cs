using InfinityMercsApp.ViewModels.Company.Viewer;

namespace InfinityMercsApp.Views;

public partial class CompanyViewerPage : ContentPageBase
{
    public CompanyViewerPage(CompanyViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
