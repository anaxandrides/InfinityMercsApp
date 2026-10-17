using InfinityMercsApp.ViewModels.About;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.About;

public partial class AboutPage : ContentPageBase
{
    public AboutPage(AboutPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
