using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Presentation.Company;

/// <summary>
/// The company cards, all the same size: one column on phones, two on medium screens and three on wider ones. The
/// grid fills the width up to <see cref="MaximumWidth"/> and is centred beyond it; a centred grid would size each
/// column to its widest title instead.
/// </summary>
public sealed record CreateNewCompanyLayout(int Columns, double MaximumWidth, CompanyCardStyle Card)
{
    public static CreateNewCompanyLayout For(AdaptiveLayoutMode mode) => mode switch
    {
        AdaptiveLayoutMode.Compact => new(1, double.PositiveInfinity, CompanyCardStyle.For(mode)),
        AdaptiveLayoutMode.Medium => new(2, 760, CompanyCardStyle.For(mode)),
        _ => new(3, 1100, CompanyCardStyle.For(mode))
    };

    /// <summary>Equal-width columns, e.g. "*,*,*".</summary>
    public string GridColumns => string.Join(",", Enumerable.Repeat("*", Columns));

    /// <summary>
    /// Enough rows for <paramref name="cardCount"/> cards. They are star rows, which in the scrolling list all take
    /// the tallest card's height.
    /// </summary>
    public string GridRowsFor(int cardCount) => string.Join(",", Enumerable.Repeat("*", (cardCount + Columns - 1) / Columns));

    /// <summary>The card at <paramref name="index"/>, filling rows left to right.</summary>
    public GridCell CellFor(int index) => new(index / Columns, index % Columns);
}
