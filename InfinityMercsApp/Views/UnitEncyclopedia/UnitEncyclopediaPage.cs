using System.ComponentModel;
using InfinityMercsApp.ViewModels;
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
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		ApplyLayout();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await InitializeViewModelAsync();
	}

	protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(UnitEncyclopediaViewModel.Step) && IsCompact)
		{
			ApplyLayout();
		}
	}

	private void ApplyLayout()
	{
		switch (LayoutMode)
		{
			case AdaptiveLayoutMode.Compact:
				// Single workspace; one step visible at a time.
				RootGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
				RootGrid.RowDefinitions = [new RowDefinition(GridLength.Star)];
				RootGrid.ColumnSpacing = 0;
				RootGrid.RowSpacing = 0;
				PlacePane(FactionsPane, 0, 0, 1, 1);
				PlacePane(BrowsePane, 0, 0, 1, 1);
				PlacePane(DetailPane, 0, 0, 1, 1);
				FactionsPane.IsVisible = _viewModel.Step == BrowseStep.Factions;
				BrowsePane.IsVisible = _viewModel.Step == BrowseStep.Units;
				DetailPane.IsVisible = _viewModel.Step == BrowseStep.Detail;
				BrowseBackButton.IsVisible = true;
				DetailBackButton.IsVisible = true;
				SetOverlaySpan(1, 1);
				break;

			case AdaptiveLayoutMode.Medium:
				// Two-pane: faction/unit navigation stacked on the left, detail on the right.
				RootGrid.ColumnDefinitions =
				[
					new ColumnDefinition(new GridLength(300)),
					new ColumnDefinition(GridLength.Star)
				];
				RootGrid.RowDefinitions =
				[
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star)
				];
				RootGrid.ColumnSpacing = 12;
				RootGrid.RowSpacing = 12;
				PlacePane(FactionsPane, 0, 0, 1, 1);
				PlacePane(BrowsePane, 1, 0, 1, 1);
				PlacePane(DetailPane, 0, 1, 2, 1);
				ShowAllPanes();
				SetOverlaySpan(2, 2);
				break;

			case AdaptiveLayoutMode.Expanded:
				ApplyThreeColumn(260, new GridLength(0.7, GridUnitType.Star), new GridLength(1.3, GridUnitType.Star));
				break;

			default: // Wide
				ApplyThreeColumn(280, new GridLength(340), GridLength.Star);
				break;
		}
	}

	private void ApplyThreeColumn(double factionWidth, GridLength browseWidth, GridLength detailWidth)
	{
		RootGrid.ColumnDefinitions =
		[
			new ColumnDefinition(new GridLength(factionWidth)),
			new ColumnDefinition(browseWidth),
			new ColumnDefinition(detailWidth)
		];
		RootGrid.RowDefinitions = [new RowDefinition(GridLength.Star)];
		RootGrid.ColumnSpacing = 12;
		RootGrid.RowSpacing = 0;
		PlacePane(FactionsPane, 0, 0, 1, 1);
		PlacePane(BrowsePane, 0, 1, 1, 1);
		PlacePane(DetailPane, 0, 2, 1, 1);
		ShowAllPanes();
		SetOverlaySpan(1, 3);
	}

	private void ShowAllPanes()
	{
		FactionsPane.IsVisible = true;
		BrowsePane.IsVisible = true;
		DetailPane.IsVisible = true;
		BrowseBackButton.IsVisible = false;
		DetailBackButton.IsVisible = false;
	}

	private static void PlacePane(View pane, int row, int column, int rowSpan, int columnSpan)
	{
		Grid.SetRow(pane, row);
		Grid.SetColumn(pane, column);
		Grid.SetRowSpan(pane, rowSpan);
		Grid.SetColumnSpan(pane, columnSpan);
	}

	private void SetOverlaySpan(int rowSpan, int columnSpan)
	{
		Grid.SetRow(UnitFilterOverlay, 0);
		Grid.SetColumn(UnitFilterOverlay, 0);
		Grid.SetRowSpan(UnitFilterOverlay, rowSpan);
		Grid.SetColumnSpan(UnitFilterOverlay, columnSpan);
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

	private async void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			var popup = new UnitFilterPopupView(
				await _viewModel.BuildUnitFilterPopupOptionsAsync(),
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
