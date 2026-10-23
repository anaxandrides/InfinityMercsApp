namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// The strip of stat headers (MOV, CC, …) over a statline. When it is sized it fits the headers' font, and the values'
/// in <see cref="Values"/>, to its height and to the width each of its <see cref="Columns"/> gets. It does so by
/// overriding <see cref="OnSizeAllocated"/>, so no event is involved.
/// </summary>
public sealed class StatLineFitBorder : Border
{
    public const double DefaultFontSize = 10d;

    private const double SmallestFontSize = 6d;
    private const double ColumnSpacing = 6d;
    private const double HorizontalPadding = 16d;

    private double _headerFontSize = DefaultFontSize;
    private double _valueFontSize = DefaultFontSize;

    /// <summary>The stat header labels' layout; the border's content.</summary>
    public Layout? Headers { get; set; }

    /// <summary>The stat value labels' layout, sized to match.</summary>
    public Layout? Values { get; set; }

    /// <summary>How many stats show: 10 with AVA, 9 without.</summary>
    public int Columns { get; set; } = 10;

    /// <summary>Gives the headers and values these font sizes.</summary>
    public void ApplyFontSizes(double headerFontSize, double valueFontSize)
    {
        _headerFontSize = headerFontSize;
        _valueFontSize = valueFontSize;
        foreach (var label in Headers?.Children.OfType<Label>() ?? [])
        {
            label.FontSize = headerFontSize;
        }

        foreach (var label in Values?.Children.OfType<Label>() ?? [])
        {
            label.FontSize = valueFontSize;
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        // The border sits in an Auto row, so its height grows with the font set here; capping by the width-derived
        // size and leaving a settled size alone (below) lets that loop converge.
        var fontFromHeight = (height - 4) * 0.75;
        var columnWidth = (width - HorizontalPadding - (ColumnSpacing * (Columns - 1))) / Columns;

        // Headers allow 3.2 characters' width for wide symbols such as the ▲ in the vitality header; values allow four
        // bold characters, about 2.6 times the font size.
        var headerFontSize = Math.Max(SmallestFontSize, Math.Round(Math.Min(fontFromHeight, columnWidth / 3.2), 1));
        var valueFontSize = Math.Max(SmallestFontSize, Math.Round(Math.Min(fontFromHeight, columnWidth / 2.6), 1));
        if (Math.Abs(_headerFontSize - headerFontSize) < 0.5 && Math.Abs(_valueFontSize - valueFontSize) < 0.5)
        {
            return;
        }

        ApplyFontSizes(headerFontSize, valueFontSize);
    }
}
