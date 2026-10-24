using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Presentation.Season;

/// <summary>
/// The season home page: the brand above the actions, which are anchored near the bottom (centred at a readable
/// width on medium screens); expanded and wider split the brand on the left and the actions on the right.
/// </summary>
public sealed record MercsSeasonLayout(
    string Columns,
    string Rows,
    GridCell ActionCell,
    Inset ActionPadding,
    Alignment ActionVerticalOptions,
    Alignment ActionHorizontalOptions,
    double ActionMaximumWidth)
{
    public static MercsSeasonLayout For(AdaptiveLayoutMode mode) => mode switch
    {
        AdaptiveLayoutMode.Compact => Stacked(Alignment.Fill, double.PositiveInfinity),
        AdaptiveLayoutMode.Medium => Stacked(Alignment.Center, 520),
        _ => new("*,*", "*", new(0, 1), Inset.Of(24, 0), Alignment.Center, Alignment.Center, 420)
    };

    private static MercsSeasonLayout Stacked(Alignment horizontalOptions, double maximumWidth) =>
        new("*", "*,Auto", new(1, 0), new Inset(24, 0, 24, 40), Alignment.End, horizontalOptions, maximumWidth);
}
