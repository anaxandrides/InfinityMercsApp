using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Presentation.UnitBrowser;

/// <summary>
/// The encyclopedia's factions, units and detail panes. Compact shows one step at a time with back buttons;
/// medium stacks factions above units on the left with the detail on the right; expanded and wide put all three
/// side by side. The filter overlay always covers the whole grid.
/// </summary>
public sealed record UnitEncyclopediaLayout(
    string Columns,
    string Rows,
    double ColumnSpacing,
    double RowSpacing,
    GridCell FactionsCell,
    GridCell BrowseCell,
    GridCell DetailCell,
    GridCell OverlayCell,
    bool ShowsOneStep)
{
    public static UnitEncyclopediaLayout For(AdaptiveLayoutMode mode) => mode switch
    {
        AdaptiveLayoutMode.Compact => new("*", "*", 0, 0, new(0, 0), new(0, 0), new(0, 0), new(0, 0), ShowsOneStep: true),
        AdaptiveLayoutMode.Medium => new("300,*", "*,*", 12, 12, new(0, 0), new(1, 0), new(0, 1, RowSpan: 2), new(0, 0, RowSpan: 2, ColumnSpan: 2), ShowsOneStep: false),
        AdaptiveLayoutMode.Expanded => ThreeColumns("260,0.7*,1.3*"),
        _ => ThreeColumns("280,340,*")
    };

    private static UnitEncyclopediaLayout ThreeColumns(string columns) =>
        new(columns, "*", 12, 0, new(0, 0), new(0, 1), new(0, 2), new(0, 0, ColumnSpan: 3), ShowsOneStep: false);

    /// <summary>Whether a step's pane shows: every pane shows on wider screens, only the current step's on compact.</summary>
    public bool Shows(BrowseStep pane, BrowseStep current) => !ShowsOneStep || pane == current;
}
