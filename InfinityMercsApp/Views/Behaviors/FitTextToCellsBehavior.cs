using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Views.Fonts;

namespace InfinityMercsApp.Views.Behaviors;

/// <summary>
/// Gives every label and picker in a packed row (a stat line, a row of upgrade pickers) one font size: the row's
/// usual size from <see cref="FontSizeKey"/> when there is room, smaller when a cell's text would not fit its width.
/// The labels and pickers should fill their cells, labels should not wrap, and neither should have a maximum width,
/// which would hide how wide the text wants to be. The row is fitted again whenever it or a cell is resized, or a
/// picker's choice changes, so it follows the window and the window's font sizes.
/// The fit waits until the layout pass that resized the row has finished, so every cell is measured at its final
/// width; measuring cells while the pass is still laying them out left Windows clipping text it had measured wrongly.
/// A cell needs the wider of its measured width and its text's width in the font file (<see cref="FontTextWidth"/>),
/// since Windows measures a label before its custom font has loaded and gets the narrower fallback's width.
/// After the text is resized the row is checked again, and shrunk further if a cell's text measures wider at the new
/// size than expected; that check never grows the text, so the two can't take turns.
/// </summary>
public sealed class FitTextToCellsBehavior : Behavior<Layout>
{
    public static readonly BindableProperty FontSizeKeyProperty =
        BindableProperty.Create(nameof(FontSizeKey), typeof(string), typeof(FitTextToCellsBehavior), "FontSizeBody");

    public static readonly BindableProperty SmallestFontSizeProperty =
        BindableProperty.Create(nameof(SmallestFontSize), typeof(double), typeof(FitTextToCellsBehavior), 7d);

#if WINDOWS
    // A Windows combo box's left padding, drop-down arrow and border, around its text.
    private const double PickerChrome = 52;
#else
    // Elsewhere the measured picker already counts its padding and arrow.
    private const double PickerChrome = 0;
#endif

    // Room for synthesised bold and rounding, which the font file's widths leave out.
    private const double TextAllowance = 1.05;

    private Layout? _row;
    private bool _fitQueued;
    private bool _queuedFitOnlyShrinks;

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

        QueueFit(onlyShrink: false);
    }

    private void Watch(View cell)
    {
        cell.SizeChanged += OnResized;
        if (cell is Picker picker)
        {
            picker.SelectedIndexChanged += OnResized;
        }
    }

    private void OnResized(object? sender, EventArgs e) => QueueFit(onlyShrink: false);

    // Several cells resize in one layout pass; they share a single fit once it is done.
    private void QueueFit(bool onlyShrink)
    {
        if (_row is null)
        {
            return;
        }

        if (_fitQueued)
        {
            _queuedFitOnlyShrinks &= onlyShrink;
            return;
        }

        _fitQueued = true;
        _queuedFitOnlyShrinks = onlyShrink;
        _row.Dispatcher.Dispatch(() =>
        {
            _fitQueued = false;
            if (Fit(_queuedFitOnlyShrinks))
            {
                QueueFit(onlyShrink: true);
            }
        });
    }

    /// <summary>Gives the row's text the size that fits, or only a smaller one; true when that changed it.</summary>
    private bool Fit(bool onlyShrink)
    {
        if (_row is null)
        {
            return false;
        }

        var cells = Cells(_row);
        if (cells.Count == 0)
        {
            return false;
        }

        var current = FontSizeOf(cells[0]);
        var usual = Application.Current?.Resources.TryGetValue(FontSizeKey, out var value) == true && value is double size ? size : current;
        var widths = cells
            .Select(cell => new CellText(cell.Width, NeededWidth(cell)))
            .ToList();
        var fitted = TextFit.SizeFor(usual, current, widths, SmallestFontSize);
        if (cells.All(cell => Math.Abs(FontSizeOf(cell) - fitted) < 0.25) || (onlyShrink && fitted >= current))
        {
            return false;
        }

        foreach (var cell in cells)
        {
            SetFontSize(cell, fitted);
        }

        return true;
    }

    private static double NeededWidth(View cell)
    {
        var measured = ((IView)cell).Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
        return TextWidthOf(cell) is { } textWidth ? Math.Max(measured, textWidth) : measured;
    }

    private static double? TextWidthOf(View cell) => cell switch
    {
        Label label => FontTextWidth.Of(label.Text, label.FontFamily, label.FontSize, label.FontAttributes.HasFlag(FontAttributes.Bold))
            * TextAllowance + label.Padding.HorizontalThickness,
        Picker picker when picker.SelectedIndex >= 0 && picker.SelectedIndex < picker.Items.Count =>
            FontTextWidth.Of(picker.Items[picker.SelectedIndex], picker.FontFamily, picker.FontSize, picker.FontAttributes.HasFlag(FontAttributes.Bold))
            * TextAllowance + PickerChrome,
        _ => null
    };

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
