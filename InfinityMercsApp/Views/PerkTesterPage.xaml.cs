using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class PerkTesterPage : ContentPageBase
{
    public PerkTesterPage(PerkTesterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
