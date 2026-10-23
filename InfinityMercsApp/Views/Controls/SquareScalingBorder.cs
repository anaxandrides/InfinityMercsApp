namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// A border that keeps one of its children, <see cref="Square"/>, a square a fraction (<see cref="SquareFraction"/>) of the
/// border's height, as a header keeps its icon button in proportion. It sizes the square when it is itself sized, by
/// overriding <see cref="OnSizeAllocated"/>, so no event is involved.
/// </summary>
public sealed class SquareScalingBorder : Border
{
    public static readonly BindableProperty SquareProperty =
        BindableProperty.Create(nameof(Square), typeof(View), typeof(SquareScalingBorder));

    public static readonly BindableProperty SquareFractionProperty =
        BindableProperty.Create(nameof(SquareFraction), typeof(double), typeof(SquareScalingBorder), 0.8);

    public View? Square
    {
        get => (View?)GetValue(SquareProperty);
        set => SetValue(SquareProperty, value);
    }

    /// <summary>The square's side as a fraction of the border's height.</summary>
    public double SquareFraction
    {
        get => (double)GetValue(SquareFractionProperty);
        set => SetValue(SquareFractionProperty, value);
    }

    // Re-assigning a size within a pixel of the current one would only start another layout pass, so it is left alone.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        var side = height * SquareFraction;
        if (Square is not { } square || side <= 0 ||
            (Math.Abs(square.WidthRequest - side) < 1.0 && Math.Abs(square.HeightRequest - side) < 1.0))
        {
            return;
        }

        square.WidthRequest = side;
        square.HeightRequest = side;
    }
}
