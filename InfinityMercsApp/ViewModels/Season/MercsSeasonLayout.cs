using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// The season home page: the brand above the actions, which are anchored near the bottom (centred at a readable
/// width on medium screens); expanded and wider split the brand on the left and the actions on the right.
/// </summary>
public sealed record MercsSeasonLayout(
    string Columns,
    string Rows,
    GridCell ActionCell,
    Thickness ActionPadding,
    LayoutOptions ActionVerticalOptions,
    LayoutOptions ActionHorizontalOptions,
    double ActionMaximumWidth)
{
    public static MercsSeasonLayout For(AdaptiveLayoutMode mode) => mode switch
    {
        AdaptiveLayoutMode.Compact => Stacked(LayoutOptions.Fill, double.PositiveInfinity),
        AdaptiveLayoutMode.Medium => Stacked(LayoutOptions.Center, 520),
        _ => new("*,*", "*", new(0, 1), new Thickness(24, 0), LayoutOptions.Center, LayoutOptions.Center, 420)
    };

    private static MercsSeasonLayout Stacked(LayoutOptions horizontalOptions, double maximumWidth) =>
        new("*", "*,Auto", new(1, 0), new Thickness(24, 0, 24, 40), LayoutOptions.End, horizontalOptions, maximumWidth);
}
