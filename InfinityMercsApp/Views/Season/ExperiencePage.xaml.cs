using InfinityMercsApp.ViewModels.Season;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season;

public partial class ExperiencePage : ContentPageBase
{
    public ExperiencePage(ExperienceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
