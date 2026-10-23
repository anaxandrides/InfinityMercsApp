using InfinityMercsApp.Domain.Models.Perks.Tables;

namespace InfinityMercsApp.Views.Controls.PerkTables;

/// <summary>
/// A perk table in its own viewport, with the dependency lines drawn over its cells. It fits the viewport when
/// <see cref="FitsViewport"/> is set (wide windows); otherwise it keeps a readable size and pans. It reacts to a new
/// table through its bindable property and to its own size through <see cref="OnSizeAllocated"/>, so nothing
/// subscribes to an event.
/// </summary>
public sealed class PerkTableView : ScrollView
{
    public static readonly BindableProperty TableProperty = BindableProperty.Create(
        nameof(Table), typeof(PerkTable), typeof(PerkTableView), propertyChanged: OnTableChanged);

    public static readonly BindableProperty FitsViewportProperty = BindableProperty.Create(
        nameof(FitsViewport), typeof(bool), typeof(PerkTableView), false, propertyChanged: OnFitsViewportChanged);

    private readonly Grid _host;
    private readonly PerkCellsGrid _cells;
    private readonly GraphicsView _lines;
    private Size _viewport;

    public PerkTableView()
    {
        Orientation = ScrollOrientation.Both;
        _lines = new GraphicsView { InputTransparent = true };
        _cells = new PerkCellsGrid(_lines);
        _host = new Grid { HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start };
        _host.Children.Add(_cells);
        _host.Children.Add(_lines);
        Content = _host;
    }

    public PerkTable? Table
    {
        get => (PerkTable?)GetValue(TableProperty);
        set => SetValue(TableProperty, value);
    }

    /// <summary>Whether the table squeezes into the viewport rather than keeping a readable size and panning.</summary>
    public bool FitsViewport
    {
        get => (bool)GetValue(FitsViewportProperty);
        set => SetValue(FitsViewportProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        _viewport = new Size(width - Padding.HorizontalThickness, height - Padding.VerticalThickness);
        SizeTable();
    }

    private static void OnTableChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var view = (PerkTableView)bindable;
        view._cells.Show(newValue as PerkTable);
        view.SizeTable();
    }

    private static void OnFitsViewportChanged(BindableObject bindable, object? oldValue, object? newValue) =>
        ((PerkTableView)bindable).SizeTable();

    // The table fills the viewport, and when it pans it never shrinks below its readable size.
    private void SizeTable()
    {
        if (_viewport.Width <= 0 || _viewport.Height <= 0)
        {
            return;
        }

        var minimum = _cells.MinimumTableSize;
        var width = FitsViewport ? _viewport.Width : Math.Max(_viewport.Width, minimum.Width);
        var height = FitsViewport ? _viewport.Height : Math.Max(_viewport.Height, minimum.Height);
        foreach (var view in new List<View> { _host, _cells, _lines })
        {
            view.WidthRequest = width;
            view.HeightRequest = height;
        }
    }
}
