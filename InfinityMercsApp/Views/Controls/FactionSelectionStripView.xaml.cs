using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Horizontal faction picker strip. Backed by a virtualized <see cref="CollectionView"/> so only the
/// on-screen faction tiles are realised; tapping a tile runs <see cref="SelectFactionCommand"/>.
/// Hiding the strip keeps its tiles: dropping them while a tap is still running left each tile's command binding
/// without its strip, and the binding threw.
/// </summary>
public partial class FactionSelectionStripView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(FactionSelectionStripView));

    public static readonly BindableProperty SelectFactionCommandProperty =
        BindableProperty.Create(nameof(SelectFactionCommand), typeof(ICommand), typeof(FactionSelectionStripView));

    public FactionSelectionStripView()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? SelectFactionCommand
    {
        get => (ICommand?)GetValue(SelectFactionCommandProperty);
        set => SetValue(SelectFactionCommandProperty, value);
    }
}
