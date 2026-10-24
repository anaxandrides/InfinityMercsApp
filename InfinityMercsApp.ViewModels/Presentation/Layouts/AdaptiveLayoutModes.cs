namespace InfinityMercsApp.Presentation.Layouts;

/// <summary>
/// Which <see cref="AdaptiveLayoutMode"/> a width falls in (logical units, from AdaptiveLayoutDefinitions.md), the same in
/// the app and on the web site.
/// </summary>
public static class AdaptiveLayoutModes
{
    public const double MediumMinWidth = 600d;
    public const double ExpandedMinWidth = 900d;
    public const double WideMinWidth = 1200d;

    public static AdaptiveLayoutMode For(double width) => width switch
    {
        >= WideMinWidth => AdaptiveLayoutMode.Wide,
        >= ExpandedMinWidth => AdaptiveLayoutMode.Expanded,
        >= MediumMinWidth => AdaptiveLayoutMode.Medium,
        _ => AdaptiveLayoutMode.Compact
    };
}
