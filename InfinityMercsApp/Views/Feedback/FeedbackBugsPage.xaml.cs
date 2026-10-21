using InfinityMercsApp.ViewModels.Feedback;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Feedback;

public partial class FeedbackBugsPage : ContentPageBase
{
    public FeedbackBugsPage(FeedbackBugsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
