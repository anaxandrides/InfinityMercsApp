using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views;

public partial class SettingsPage : ContentPageBase
{
	public SettingsPage(SettingsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
