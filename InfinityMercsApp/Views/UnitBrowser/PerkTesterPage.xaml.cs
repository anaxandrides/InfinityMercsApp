using InfinityMercsApp.ViewModels.UnitBrowser;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.UnitBrowser;

public partial class PerkTesterPage : ContentPageBase
{
    public PerkTesterPage(PerkTesterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
