using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>A trooper's tile in a unit strip; its BindingContext is an <c>IUnitTile</c>.</summary>
public partial class CompanyViewerUnitTileView : ContentView
{
    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(nameof(ItemTappedCommand), typeof(ICommand), typeof(CompanyViewerUnitTileView));

    /// <summary>The outline of the selected tile; the Signal green unless the page gives a faction accent.</summary>
    public static readonly BindableProperty SelectedStrokeProperty =
        BindableProperty.Create(nameof(SelectedStroke), typeof(Color), typeof(CompanyViewerUnitTileView), Color.FromArgb("#34D399"));

    /// <summary>Runs with the horizontal distance dragged since the last update, to scroll the strip.</summary>
    public static readonly BindableProperty PanScrollCommandProperty =
        BindableProperty.Create(nameof(PanScrollCommand), typeof(ICommand), typeof(CompanyViewerUnitTileView));

    public CompanyViewerUnitTileView()
    {
        InitializeComponent();
    }

    /// <summary>Runs with the tile's item when it is tapped.</summary>
    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    public Color SelectedStroke
    {
        get => (Color)GetValue(SelectedStrokeProperty);
        set => SetValue(SelectedStrokeProperty, value);
    }

    public ICommand? PanScrollCommand
    {
        get => (ICommand?)GetValue(PanScrollCommandProperty);
        set => SetValue(PanScrollCommandProperty, value);
    }
}
