using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views;

public partial class AboutPage : ContentPageBase
{
    public AboutPage(AboutPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
