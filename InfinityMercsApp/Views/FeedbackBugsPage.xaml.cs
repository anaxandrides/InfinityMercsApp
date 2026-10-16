using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views;

public partial class FeedbackBugsPage : ContentPageBase
{
    public FeedbackBugsPage(FeedbackBugsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
