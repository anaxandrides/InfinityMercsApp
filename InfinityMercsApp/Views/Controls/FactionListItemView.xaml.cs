using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// A faction tile (centred logo over the name) for the faction strip and the compact faction selector;
/// its BindingContext is an <c>ArmyFactionSelectionItem</c>.
/// </summary>
public partial class FactionListItemView : ContentView
{
    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(nameof(ItemTappedCommand), typeof(ICommand), typeof(FactionListItemView));

    public FactionListItemView()
    {
        InitializeComponent();
    }

    /// <summary>Runs with the tile's faction when it is tapped.</summary>
    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }
}
