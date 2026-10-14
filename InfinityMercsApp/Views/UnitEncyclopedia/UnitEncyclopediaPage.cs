using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.UnitEncyclopedia;

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
