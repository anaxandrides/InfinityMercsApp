using System.Runtime.CompilerServices;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// A one-line label that steps its font down from <see cref="MaximumFontSize"/> until its text fits its width, and
/// back up when there is room. It refits when it is sized and when its text changes, by overriding
/// <see cref="OnSizeAllocated"/> and <see cref="OnPropertyChanged"/>, so no event is involved.
/// </summary>
public sealed class ShrinkToFitLabel : Label
{
    public static readonly BindableProperty MaximumFontSizeProperty =
        BindableProperty.Create(nameof(MaximumFontSize), typeof(double), typeof(ShrinkToFitLabel), 24d);

    public static readonly BindableProperty MinimumFontSizeProperty =
        BindableProperty.Create(nameof(MinimumFontSize), typeof(double), typeof(ShrinkToFitLabel), 11d);

    private const double FontStep = 0.5d;

    // Setting the font re-measures the label, which sizes it again; that pass leaves the fitted size alone.
    private bool _fitting;

    public ShrinkToFitLabel()
    {
        LineBreakMode = LineBreakMode.NoWrap;
        MaxLines = 1;
    }

    public double MaximumFontSize
    {
        get => (double)GetValue(MaximumFontSizeProperty);
        set => SetValue(MaximumFontSizeProperty, value);
    }

    public double MinimumFontSize
    {
        get => (double)GetValue(MinimumFontSizeProperty);
        set => SetValue(MinimumFontSizeProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        Fit();
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == TextProperty.PropertyName || propertyName == MaximumFontSizeProperty.PropertyName)
        {
            Fit();
        }
    }

    private void Fit()
    {
        if (_fitting)
        {
            return;
        }

        _fitting = true;
        try
        {
            if (Width <= 0)
            {
                FontSize = MaximumFontSize;
                return;
            }

            for (var size = MaximumFontSize; size >= MinimumFontSize; size -= FontStep)
            {
                FontSize = size;
                if (Measure(double.PositiveInfinity, double.PositiveInfinity).Width <= Width)
                {
                    return;
                }
            }

            FontSize = MinimumFontSize;
        }
        finally
        {
            _fitting = false;
        }
    }
}
