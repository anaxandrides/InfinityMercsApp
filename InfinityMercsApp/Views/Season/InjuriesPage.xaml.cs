using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.Season;

public partial class InjuriesPage : ContentPageBase
{
    public InjuriesPage(InjuriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
