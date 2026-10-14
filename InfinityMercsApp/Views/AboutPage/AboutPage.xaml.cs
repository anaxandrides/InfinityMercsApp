using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class AboutPage : ContentPageBase
{
    public AboutPage(AboutPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
