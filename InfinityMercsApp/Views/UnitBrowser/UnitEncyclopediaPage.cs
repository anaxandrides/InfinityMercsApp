using InfinityMercsApp.ViewModels.UnitBrowser;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.UnitBrowser;

public partial class UnitEncyclopediaPage : AdaptiveContentPage
{
	/// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
	public UnitEncyclopediaViewModel ViewModel { get; }

	public UnitEncyclopediaPage(UnitEncyclopediaViewModel viewModel)
	{
		ViewModel = viewModel;
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await InitializeViewModelAsync();
	}
}
