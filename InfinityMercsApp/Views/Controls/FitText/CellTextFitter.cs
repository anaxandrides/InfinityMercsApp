using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Views.Fonts;

namespace InfinityMercsApp.Views.Controls.FitText;

/// <summary>
/// Gives every label and picker in a packed row (a stat line, a row of upgrade pickers) one font size: the row's
/// usual size from its font size key when there is room, smaller when a cell's text would not fit its width. The row
/// calls <see cref="Arranged"/> each time it is arranged, which happens whenever it, a cell or a picker's choice
/// changes, so it follows the window and the window's font sizes without listening to any event.
/// The fit waits until the layout pass has finished, so every cell is measured at its final width; measuring cells
/// while the pass is still laying them out left Windows clipping text it had measured wrongly.
/// A cell needs the wider of its measured width and its text's width in the font file (<see cref="FontTextWidth"/>),
/// since Windows can measure a label before its bundled font has loaded and get the fallback's width.
/// After the text is resized the row is checked again, and shrunk further if a cell's text measures wider at the new
/// size than expected; that check never grows the text, so the two can't take turns.
/// The labels and pickers should fill their cells, labels should not wrap, and neither should have a maximum width,
/// which would hide how wide the text wants to be.
/// </summary>
public sealed class CellTextFitter(Layout row)
{
#if WINDOWS
    // A Windows combo box's left padding, drop-down arrow and border, around its text.
    private const double PickerChrome = 52;
#else
    // Elsewhere the measured picker already counts its padding and arrow.
    private const double PickerChrome = 0;
#endif

    // Room for synthesised bold and rounding, which the font file's widths leave out.
    private const double TextAllowance = 1.05;

    private bool _fitQueued;
    private bool _nextFitOnlyShrinks;

    /// <summary>The shared font size resource the row uses when there is room.</summary>
    public string FontSizeKey { get; set; } = "FontSizeBody";

    public double SmallestFontSize { get; set; } = 7d;

    /// <summary>Fits the row once the layout pass that arranged it has finished.</summary>
    public void Arranged()
    {
        if (_fitQueued)
        {
            return;
        }

        _fitQueued = true;
        row.Dispatcher.Dispatch(() =>
        {
            _fitQueued = false;
            var onlyShrink = _nextFitOnlyShrinks;
            // A changed size arranges the row again; that pass may only shrink the text further.
            _nextFitOnlyShrinks = Fit(onlyShrink);
        });
    }

    /// <summary>Gives the row's text the size that fits, or only a smaller one; true when that changed it.</summary>
    private bool Fit(bool onlyShrink)
    {
        var cells = Cells();
        if (cells.Count == 0)
        {
            return false;
        }

        var current = FontSizeOf(cells[0]);
        var usual = Application.Current?.Resources.TryGetValue(FontSizeKey, out var value) == true && value is double size ? size : current;
        var widths = cells.Select(cell => new CellText(cell.Width, NeededWidth(cell))).ToList();
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

    private List<View> Cells() =>
        row.GetVisualTreeDescendants().OfType<View>().Where(view => view is Label or Picker).ToList();
}
