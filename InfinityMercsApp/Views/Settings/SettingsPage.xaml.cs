using InfinityMercsApp.ViewModels.Settings;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Settings;

public partial class SettingsPage : ContentPageBase
{
	public SettingsPage(SettingsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
