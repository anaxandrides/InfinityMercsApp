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

    private double _panLastTotalX;

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

    private void OnTilePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panLastTotalX = 0d;
                break;
            case GestureStatus.Running:
                var deltaX = e.TotalX - _panLastTotalX;
                _panLastTotalX = e.TotalX;
                if (PanScrollCommand?.CanExecute(deltaX) == true)
                {
                    PanScrollCommand.Execute(deltaX);
                }

                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panLastTotalX = 0d;
                break;
        }
    }
}
