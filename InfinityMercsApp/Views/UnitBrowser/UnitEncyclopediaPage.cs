using InfinityMercsApp.ViewModels.UnitBrowser;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.UnitBrowser;

public partial class UnitEncyclopediaPage : AdaptiveContentPage
{
	public UnitEncyclopediaPage(UnitEncyclopediaViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await InitializeViewModelAsync();
	}
}
