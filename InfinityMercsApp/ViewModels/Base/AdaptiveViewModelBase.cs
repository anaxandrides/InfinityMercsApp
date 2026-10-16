using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Layouts;

namespace InfinityMercsApp.ViewModels.Base;

/// <summary>
/// A ViewModel whose page lays itself out by width. The page's XAML forwards its
/// <c>LayoutModeChanged</c> event to <see cref="ApplyLayoutCommand"/> with an <c>EventToCommandBehavior</c>,
/// and binds to <see cref="Layout"/>, which <paramref name="layoutFor"/> builds for each mode.
/// Pages start out compact until their first width is known.
/// </summary>
public abstract partial class AdaptiveViewModelBase<TLayout>(
    INavigationService navigationService,
    Func<AdaptiveLayoutMode, TLayout> layoutFor) : ViewModelBase(navigationService)
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompact), nameof(IsWide))]
    public partial AdaptiveLayoutMode LayoutMode { get; set; }

    [ObservableProperty]
    public partial TLayout Layout { get; set; } = layoutFor(AdaptiveLayoutMode.Compact);

    public bool IsCompact => LayoutMode == AdaptiveLayoutMode.Compact;
    public bool IsWide => LayoutMode == AdaptiveLayoutMode.Wide;

    [RelayCommand]
    private void ApplyLayout(AdaptiveLayoutMode mode)
    {
        LayoutMode = mode;
        Layout = layoutFor(mode);
        OnLayoutApplied();
    }

    /// <summary>Override to work out again the values that combine the layout with other state.</summary>
    protected virtual void OnLayoutApplied()
    {
    }
}
