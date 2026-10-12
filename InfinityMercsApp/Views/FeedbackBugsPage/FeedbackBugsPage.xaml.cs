using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class FeedbackBugsPage : ContentPageBase
{
    public FeedbackBugsPage(FeedbackBugsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
