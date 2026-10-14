using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>
/// The inside of a company card. Phones lay the icon and title out side by side in a short row; wider screens
/// stack a large icon above the centred title.
/// </summary>
public sealed record CompanyCardStyle(
    string Columns,
    string Rows,
    double ColumnSpacing,
    double RowSpacing,
    GridCell IconCell,
    GridCell TitleCell,
    double IconSize,
    LayoutOptions TitleVerticalOptions,
    LayoutOptions TitleHorizontalOptions,
    TextAlignment TitleTextAlignment,
    bool UsesCompactTitle,
    double MinimumHeight)
{
    public static CompanyCardStyle For(AdaptiveLayoutMode mode) => mode == AdaptiveLayoutMode.Compact
        ? new("Auto,*", "Auto", 12, 0, new(0, 0), new(0, 1), 56, LayoutOptions.Center, LayoutOptions.Start, TextAlignment.Start, UsesCompactTitle: true, 72)
        : new("*", "*,Auto", 0, 6, new(0, 0), new(1, 0), 120, LayoutOptions.Start, LayoutOptions.Center, TextAlignment.Center, UsesCompactTitle: false, 160);
}
