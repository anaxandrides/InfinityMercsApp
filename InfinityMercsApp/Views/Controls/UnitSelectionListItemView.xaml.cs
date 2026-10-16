using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>A unit row of a company selection list or roster card: logo, name and subtitle. Its BindingContext is an <c>IViewerListItem</c>.</summary>
public partial class UnitSelectionListItemView : ContentView
{
    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(nameof(ItemTappedCommand), typeof(ICommand), typeof(UnitSelectionListItemView));

    /// <summary>Picks the row out in blue, e.g. the TAG Company's own TAG.</summary>
    public static readonly BindableProperty IsHighlightedProperty =
        BindableProperty.Create(nameof(IsHighlighted), typeof(bool), typeof(UnitSelectionListItemView), false);

    public UnitSelectionListItemView()
    {
        InitializeComponent();
    }

    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    /// <summary>Runs with the row's item when it is tapped.</summary>
    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }
}
