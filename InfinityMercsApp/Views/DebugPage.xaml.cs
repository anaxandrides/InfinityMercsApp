using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views;

public partial class DebugPage : ContentPageBase
{
    public DebugPage(DebugViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
