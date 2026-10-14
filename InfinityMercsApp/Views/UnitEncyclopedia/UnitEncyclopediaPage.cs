using System.ComponentModel;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Adaptive;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.UnitEncyclopedia;

/// <summary>
/// Faction, unit and fireteam browser. <see cref="UnitEncyclopediaViewModel"/> holds the data and the
/// phone browse step; the code-behind arranges the panes, hosts the filter popup and drag-scrolls lists.
/// </summary>
public partial class UnitEncyclopediaPage : AdaptiveContentPage
{
	private readonly UnitEncyclopediaViewModel _viewModel;
	private double _factionDragStartScrollY;
	private double _unitDragStartScrollY;
	private UnitFilterPopupView? _activeUnitFilterPopup;

	public UnitEncyclopediaPage(UnitEncyclopediaViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await InitializeViewModelAsync();
	}

	// Mouse/touch drag scrolls the lists (desktop has no touch scrolling).
	private async void OnFactionListPanUpdated(object? sender, PanUpdatedEventArgs e) =>
		_factionDragStartScrollY = await DragScrollAsync(FactionScrollView, e, _factionDragStartScrollY);

	private async void OnUnitListPanUpdated(object? sender, PanUpdatedEventArgs e) =>
		_unitDragStartScrollY = await DragScrollAsync(UnitScrollView, e, _unitDragStartScrollY);

	private static async Task<double> DragScrollAsync(ScrollView scrollView, PanUpdatedEventArgs e, double startY)
	{
		switch (e.StatusType)
		{
			case GestureStatus.Started:
				return scrollView.ScrollY;
			case GestureStatus.Running:
				await scrollView.ScrollToAsync(0, Math.Max(0, startY - e.TotalY), false);
				break;
		}

		return startY;
	}

	private void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			var popup = new UnitFilterPopupView(
				_viewModel.BuildUnitFilterPopupOptions(),
				_viewModel.ActiveUnitFilter,
				lieutenantOnlyUnits: _viewModel.LieutenantOnlyUnits,
				teamsView: false);
			var popupHeight = Height > 0 ? Height * 0.9 : 800;
			popup.HeightRequest = popupHeight;
			popup.FilterArmyApplied += OnFilterArmyApplied;
			popup.CloseRequested += OnUnitFilterPopupCloseRequested;
			_activeUnitFilterPopup = popup;
			UnitFilterPopupHost.HeightRequest = popupHeight;
			UnitFilterPopupHost.Content = popup;
			UnitFilterOverlay.IsVisible = true;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage filter popup open failed: {ex.Message}");
		}
	}

	private async void OnFilterArmyApplied(object? sender, UnitFilterCriteria criteria)
	{
		CloseUnitFilterPopup();
		await _viewModel.ApplyUnitFilterCommand.ExecuteAsync(criteria);
	}

	private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e) => CloseUnitFilterPopup();

	private void CloseUnitFilterPopup()
	{
		if (_activeUnitFilterPopup is not null)
		{
			_activeUnitFilterPopup.FilterArmyApplied -= OnFilterArmyApplied;
			_activeUnitFilterPopup.CloseRequested -= OnUnitFilterPopupCloseRequested;
		}

		_activeUnitFilterPopup = null;
		UnitFilterPopupHost.Content = null;
		UnitFilterPopupHost.HeightRequest = -1;
		UnitFilterOverlay.IsVisible = false;
	}
}
