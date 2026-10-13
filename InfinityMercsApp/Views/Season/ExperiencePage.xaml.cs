using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.Season;

public partial class ExperiencePage : ContentPageBase
{
    public ExperiencePage(ExperienceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
