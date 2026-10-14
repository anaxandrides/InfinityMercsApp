using CommunityToolkit.Mvvm.Input;
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

/// <summary>The company cards: one column on phones, two on medium screens and three on wider ones, centred and capped.</summary>
public sealed record CreateNewCompanyLayout(int Columns, LayoutOptions HorizontalOptions, double MaximumWidth, CompanyCardStyle Card)
{
    public static CreateNewCompanyLayout For(AdaptiveLayoutMode mode) => mode switch
    {
        AdaptiveLayoutMode.Compact => new(1, LayoutOptions.Fill, double.PositiveInfinity, CompanyCardStyle.For(mode)),
        AdaptiveLayoutMode.Medium => new(2, LayoutOptions.Center, 760, CompanyCardStyle.For(mode)),
        _ => new(3, LayoutOptions.Center, 1100, CompanyCardStyle.For(mode))
    };

    /// <summary>Equal-width columns, e.g. "*,*,*".</summary>
    public string GridColumns => string.Join(",", Enumerable.Repeat("*", Columns));

    /// <summary>Enough auto-height rows for <paramref name="cardCount"/> cards.</summary>
    public string GridRowsFor(int cardCount) => string.Join(",", Enumerable.Repeat("Auto", (cardCount + Columns - 1) / Columns));

    /// <summary>The card at <paramref name="index"/>, filling rows left to right.</summary>
    public GridCell CellFor(int index) => new(index / Columns, index % Columns);
}

/// <summary>A company card: its title and icon, the command that opens it, and where it sits.</summary>
public sealed record CompanyChoice(string Title, string IconSource, IAsyncRelayCommand Command, GridCell Cell, CompanyCardStyle Style);
