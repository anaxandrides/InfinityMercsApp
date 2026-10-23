namespace InfinityMercsApp.Views.Controls.FitText;

/// <summary>A vertical stack whose labels and pickers share one font size that fits every cell (<see cref="CellTextFitter"/>).</summary>
public sealed class FitTextStackLayout : VerticalStackLayout
{
    private readonly CellTextFitter _fitter;

    public FitTextStackLayout() => _fitter = new CellTextFitter(this);

    /// <inheritdoc cref="CellTextFitter.FontSizeKey"/>
    public string FontSizeKey
    {
        get => _fitter.FontSizeKey;
        set => _fitter.FontSizeKey = value;
    }

    protected override Size ArrangeOverride(Rect bounds)
    {
        var size = base.ArrangeOverride(bounds);
        _fitter.Arranged();
        return size;
    }
}
