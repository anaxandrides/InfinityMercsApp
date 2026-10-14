using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>
/// A list beside its detail. Compact shows one pane at a time with a back button on the detail; wider screens
/// put the list in a fixed-width column beside the detail, and the widest cap the detail at a reading width.
/// </summary>
public sealed record SplitPaneLayout(string Columns, double ColumnSpacing, GridCell DetailCell, bool StacksPanes, double ReadingMaxWidth)
{
    public static SplitPaneLayout For(AdaptiveLayoutMode mode, SplitPaneSizes sizes) => mode switch
    {
        AdaptiveLayoutMode.Compact => new("*", 0, new(0, 0), StacksPanes: true, double.PositiveInfinity),
        AdaptiveLayoutMode.Medium => Split(sizes.MediumListWidth, double.PositiveInfinity),
        AdaptiveLayoutMode.Expanded => Split(sizes.ExpandedListWidth, double.PositiveInfinity),
        _ => Split(sizes.WideListWidth, sizes.WideReadingWidth)
    };

    private static SplitPaneLayout Split(double listWidth, double readingWidth) =>
        new(FormattableString.Invariant($"{listWidth},*"), 16, new(0, 1), StacksPanes: false, readingWidth);

    /// <summary>Whether the list shows: always beside the detail, or on its own until a detail is opened.</summary>
    public bool ShowsList(bool isShowingDetail) => !StacksPanes || !isShowingDetail;

    /// <summary>Whether the detail shows: always beside the list, or on its own once opened.</summary>
    public bool ShowsDetail(bool isShowingDetail) => !StacksPanes || isShowingDetail;
}
