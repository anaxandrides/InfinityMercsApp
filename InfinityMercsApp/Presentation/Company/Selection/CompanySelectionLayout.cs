using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Presentation.Company.Selection;

/// <summary>
/// How a company selection screen splits its width: compact shows one workspace at a time beneath a tab bar;
/// wider screens put the units beside the unit detail stacked over the company, the units rail narrowing
/// and then fixing as width grows.
/// </summary>
public sealed record CompanySelectionLayout(string Columns, double ColumnSpacing, bool IsCompact)
{
    public static CompanySelectionLayout For(AdaptiveLayoutMode mode) => mode switch
    {
        AdaptiveLayoutMode.Compact => new("*", 0, IsCompact: true),
        AdaptiveLayoutMode.Medium => new("0.82*,1.18*", 12, IsCompact: false),
        AdaptiveLayoutMode.Expanded => new("0.72*,1.28*", 12, IsCompact: false),
        _ => new("380,*", 12, IsCompact: false)
    };

    /// <summary>The right column's column: beside the units, or in their place when compact.</summary>
    public GridCell RightPaneCell => IsCompact ? new GridCell(2, 0) : new GridCell(2, 1);

    public bool ShowsUnits(CompanySelectionPane pane) => !IsCompact || pane == CompanySelectionPane.Units;

    public bool ShowsRightPane(CompanySelectionPane pane) => !IsCompact || pane != CompanySelectionPane.Units;

    /// <summary>The unit detail: its tab when compact; otherwise only once a unit is selected, so an empty one takes no room.</summary>
    public bool ShowsDetail(CompanySelectionPane pane, bool hasSelectedUnit) =>
        IsCompact ? pane == CompanySelectionPane.Profile : hasSelectedUnit;

    public bool ShowsCompany(CompanySelectionPane pane) => !IsCompact || pane == CompanySelectionPane.Company;

    /// <summary>
    /// The right column's rows: detail over company with a gutter, or all the height to the company until a unit is
    /// selected; all the height to the one shown when compact.
    /// </summary>
    public string RightPaneRows(CompanySelectionPane pane, bool hasSelectedUnit) =>
        !IsCompact ? hasSelectedUnit ? "*,3,*" : "0,0,*"
        : pane == CompanySelectionPane.Company ? "0,0,*"
        : "*,0,0";
}
