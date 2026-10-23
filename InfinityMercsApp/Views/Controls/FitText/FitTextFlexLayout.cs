namespace InfinityMercsApp.Views.Controls.FitText;

/// <summary>A flex row whose labels and pickers share one font size that fits every cell (<see cref="CellTextFitter"/>).</summary>
public sealed class FitTextFlexLayout : FlexLayout
{
    private readonly CellTextFitter _fitter;

    public FitTextFlexLayout() => _fitter = new CellTextFitter(this);

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
