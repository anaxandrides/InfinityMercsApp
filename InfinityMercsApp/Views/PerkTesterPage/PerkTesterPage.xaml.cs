using InfinityMercsApp.ViewModels.UnitBrowser;

namespace InfinityMercsApp.Views;

public partial class PerkTesterPage : ContentPageBase
{
    public PerkTesterPage(PerkTesterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
