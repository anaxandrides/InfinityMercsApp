using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.Presentation.Captain;

/// <summary>
/// How the captain popup arranges its two panes, each scrolling on its own. Compact stacks the upgrades under the
/// captain, each pane half the card, with the buttons across the full width; wider screens put the panes side by
/// side with the buttons under the captain.
/// </summary>
public sealed record CaptainPopupLayout(
    string Columns,
    string Rows,
    double ColumnSpacing,
    double RowSpacing,
    GridCell UpgradesCell,
    string ActionColumns)
{
    public static CaptainPopupLayout For(AdaptiveLayoutMode mode) => mode == AdaptiveLayoutMode.Compact
        ? new("*", "*,*", 0, 14, new(1, 0), "*")
        : new("*,*", "*", 18, 0, new(0, 1), "*,*");
}
