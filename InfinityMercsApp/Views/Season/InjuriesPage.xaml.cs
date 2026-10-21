using InfinityMercsApp.ViewModels.Season;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season;

public partial class InjuriesPage : ContentPageBase
{
    public InjuriesPage(InjuriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
