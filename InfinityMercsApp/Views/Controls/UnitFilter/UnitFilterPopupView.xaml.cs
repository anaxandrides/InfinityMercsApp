using InfinityMercsApp.ViewModels.Components.UnitFilter;

namespace InfinityMercsApp.Views.Controls.UnitFilter;

/// <summary>The unit filter popup; its BindingContext is the host's <c>UnitFilterViewModel</c>.</summary>
public partial class UnitFilterPopupView : ContentView
{
    // Single-column layout works at every width, so sizing is all that adapts:
    // cap the popup to a comfortable reading column and let the body ScrollView
    // absorb any overflow rather than running off a short screen.
    private const double MaxPopupWidth = 560d;
    private const double OuterPadding = 32d; // root Grid Padding="16" on both sides

    public UnitFilterPopupView()
    {
        InitializeComponent();
    }

    /// <summary>The BindingContext as its ViewModel, which item templates bind to through x:Reference.</summary>
    public UnitFilterViewModel? ViewModel => BindingContext as UnitFilterViewModel;

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        OnPropertyChanged(nameof(ViewModel));
    }

    // The popup covers its host, so its own size is the room it has.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (height > 0)
        {
            PopupContainer.MaximumHeightRequest = height * 0.9;
        }

        if (width > 0)
        {
            PopupContainer.WidthRequest = Math.Min(width - OuterPadding, MaxPopupWidth);
        }
    }
}
