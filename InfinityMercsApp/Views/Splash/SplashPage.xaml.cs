using InfinityMercsApp.ViewModels.Splash;

namespace InfinityMercsApp.Views.Splash;

public partial class SplashPage
{
    public SplashPage(SplashPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
