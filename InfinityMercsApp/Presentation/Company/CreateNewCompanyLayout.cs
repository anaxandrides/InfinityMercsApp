using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Presentation.Company;

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
