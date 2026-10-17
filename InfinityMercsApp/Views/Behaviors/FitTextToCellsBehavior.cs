using InfinityMercsApp.Presentation.Layouts;
namespace InfinityMercsApp.Views.Behaviors;

/// <summary>
/// Gives every label in a packed row (a stat line, say) one font size: the row's usual size from
/// <see cref="FontSizeKey"/> when there is room, smaller when a cell's text would not fit its width. The labels
/// should fill their cells and not wrap. The row is fitted again whenever it or a label is resized, so it follows
/// the window and the window's font sizes.
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
        foreach (var label in Labels(row))
        {
            label.SizeChanged += OnResized;
        }
    }

    protected override void OnDetachingFrom(Layout row)
    {
        row.SizeChanged -= OnResized;
        row.DescendantAdded -= OnDescendantAdded;
        foreach (var label in Labels(row))
        {
            label.SizeChanged -= OnResized;
        }

        _row = null;
        base.OnDetachingFrom(row);
    }

    private void OnDescendantAdded(object? sender, ElementEventArgs e)
    {
        if (e.Element is Label label)
        {
            label.SizeChanged += OnResized;
        }

        Fit();
    }

    private void OnResized(object? sender, EventArgs e) => Fit();

    private void Fit()
    {
        // Changing the size resizes the labels, which lands back here; that pass is skipped.
        if (_fitting || _row is null)
        {
            return;
        }

        var labels = Labels(_row);
        if (labels.Count == 0)
        {
            return;
        }

        var usual = Application.Current?.Resources.TryGetValue(FontSizeKey, out var value) == true && value is double size ? size : labels[0].FontSize;
        var current = labels[0].FontSize;
        var cells = labels
            .Select(label => new CellText(label.Width, ((IView)label).Measure(double.PositiveInfinity, double.PositiveInfinity).Width))
            .ToList();
        var fitted = TextFit.SizeFor(usual, current, cells, SmallestFontSize);
        if (labels.All(label => Math.Abs(label.FontSize - fitted) < 0.25))
        {
            return;
        }

        _fitting = true;
        try
        {
            foreach (var label in labels)
            {
                label.FontSize = fitted;
            }
        }
        finally
        {
            _fitting = false;
        }
    }

    private static IReadOnlyList<Label> Labels(Layout row) => row.GetVisualTreeDescendants().OfType<Label>().ToList();
}
