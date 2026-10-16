using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Full-screen overlay faction picker for small (Compact) screens. Presents the faction list as a
/// single column of full-width "Company Style Icon" cards (large SVG + label) that scrolls
/// vertically. Selecting a faction runs the page's <see cref="SelectFactionCommand"/>; the hosting
/// page decides when to close the overlay (via <see cref="IsOpen"/>) so multi-slot companies can
/// keep picking.
/// </summary>
public partial class FactionSelectorOverlayView : ContentView
{
    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(FactionSelectorOverlayView), false, propertyChanged: OnGatingChanged);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(FactionSelectorOverlayView), "Choose your faction");

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(FactionSelectorOverlayView), propertyChanged: OnGatingChanged);

    public static readonly BindableProperty SelectFactionCommandProperty =
        BindableProperty.Create(nameof(SelectFactionCommand), typeof(ICommand), typeof(FactionSelectorOverlayView));

    public static readonly BindableProperty CloseCommandProperty =
        BindableProperty.Create(nameof(CloseCommand), typeof(ICommand), typeof(FactionSelectorOverlayView));

    public FactionSelectorOverlayView()
    {
        InitializeComponent();
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// The item source actually bound to the inner list. The faction cards are SVG-heavy and
    /// non-virtualized, so a closed overlay binds nothing and pays no build cost until it opens.
    /// </summary>
    public IEnumerable? EffectiveItemsSource => IsOpen ? ItemsSource : null;

    private static void OnGatingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FactionSelectorOverlayView view)
        {
            view.OnPropertyChanged(nameof(EffectiveItemsSource));
        }
    }

    public ICommand? SelectFactionCommand
    {
        get => (ICommand?)GetValue(SelectFactionCommandProperty);
        set => SetValue(SelectFactionCommandProperty, value);
    }

    /// <summary>Runs when the close button is tapped.</summary>
    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }
}
