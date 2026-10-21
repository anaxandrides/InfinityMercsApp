using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Views.Behaviors;

/// <summary>
/// Gives every label and picker in a packed row (a stat line, a row of upgrade pickers) one font size: the row's
/// usual size from <see cref="FontSizeKey"/> when there is room, smaller when a cell's text would not fit its width.
/// The labels and pickers should fill their cells, labels should not wrap, and neither should have a maximum width,
/// which would hide how wide the text wants to be. The row is fitted again whenever it or a cell is resized, or a
/// picker's choice changes, so it follows the window and the window's font sizes.
/// </summary>
public sealed class FitTextToCellsBehavior : Behavior<Layout>
{
    public static readonly BindableProperty FontSizeKeyProperty =
        BindableProperty.Create(nameof(FontSizeKey), typeof(string), typeof(FitTextToCellsBehavior), "FontSizeBody");

    public static readonly BindableProperty SmallestFontSizeProperty =
        BindableProperty.Create(nameof(SmallestFontSize), typeof(double), typeof(FitTextToCellsBehavior), 7d);

    private Layout? _row;
    private bool _fitting;

    /// <summary>The shared font size resource the row uses when there is room.</summary>
    public string FontSizeKey
    {
        get => (string)GetValue(FontSizeKeyProperty);
        set => SetValue(FontSizeKeyProperty, value);
    }

    public double SmallestFontSize
    {
        get => (double)GetValue(SmallestFontSizeProperty);
        set => SetValue(SmallestFontSizeProperty, value);
    }

    protected override void OnAttachedTo(Layout row)
    {
        base.OnAttachedTo(row);
        _row = row;
        row.SizeChanged += OnResized;
        row.DescendantAdded += OnDescendantAdded;
        foreach (var cell in Cells(row))
        {
            Watch(cell);
        }
    }

    protected override void OnDetachingFrom(Layout row)
    {
        row.SizeChanged -= OnResized;
        row.DescendantAdded -= OnDescendantAdded;
        foreach (var cell in Cells(row))
        {
            cell.SizeChanged -= OnResized;
            if (cell is Picker picker)
            {
                picker.SelectedIndexChanged -= OnResized;
            }
        }

        _row = null;
        base.OnDetachingFrom(row);
    }

    private void OnDescendantAdded(object? sender, ElementEventArgs e)
    {
        if (e.Element is Label or Picker)
        {
            Watch((View)e.Element);
        }

        Fit();
    }

    private void Watch(View cell)
    {
        cell.SizeChanged += OnResized;
        if (cell is Picker picker)
        {
            picker.SelectedIndexChanged += OnResized;
        }
    }

    private void OnResized(object? sender, EventArgs e) => Fit();

    private void Fit()
    {
        // Changing the size resizes the cells, which lands back here; that pass is skipped.
        if (_fitting || _row is null)
        {
            return;
        }

        var cells = Cells(_row);
        if (cells.Count == 0)
        {
            return;
        }

        var current = FontSizeOf(cells[0]);
        var usual = Application.Current?.Resources.TryGetValue(FontSizeKey, out var value) == true && value is double size ? size : current;
        var widths = cells
            .Select(cell => new CellText(cell.Width, ((IView)cell).Measure(double.PositiveInfinity, double.PositiveInfinity).Width))
            .ToList();
        var fitted = TextFit.SizeFor(usual, current, widths, SmallestFontSize);
        if (cells.All(cell => Math.Abs(FontSizeOf(cell) - fitted) < 0.25))
        {
            return;
        }

        _fitting = true;
        try
        {
            foreach (var cell in cells)
            {
                SetFontSize(cell, fitted);
            }
        }
        finally
        {
            _fitting = false;
        }
    }

    private static double FontSizeOf(View cell) => cell switch
    {
        Picker picker => picker.FontSize,
        Label label => label.FontSize,
        _ => 0
    };

    private static void SetFontSize(View cell, double size)
    {
        if (cell is Picker picker)
        {
            picker.FontSize = size;
        }
        else if (cell is Label label)
        {
            label.FontSize = size;
        }
    }

    private static IReadOnlyList<View> Cells(Layout row) =>
        row.GetVisualTreeDescendants().OfType<View>().Where(view => view is Label or Picker).ToList();
}
