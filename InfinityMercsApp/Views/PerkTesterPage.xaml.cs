using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views;

/// <summary>
/// Checks which perks a unit configuration qualifies for. <see cref="PerkTesterViewModel"/> does the
/// work; the code-behind hosts the filter popup and drag-scrolls the lists.
/// </summary>
public partial class PerkTesterPage : ContentPageBase
{
    private readonly PerkTesterViewModel _viewModel;
    private double _factionDragStartScrollY;
    private double _unitDragStartScrollY;
    private UnitFilterPopupView? _activeUnitFilterPopup;

    public PerkTesterPage(PerkTesterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CloseUnitFilterPopup();
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
            Console.Error.WriteLine($"PerkTesterPage filter popup open failed: {ex.Message}");
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
