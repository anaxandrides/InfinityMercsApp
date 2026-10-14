using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>
/// The saved-records lists (companies, seasons): compact fills the screen; wider widths centre the list at a
/// readable width, and the widest screens reflow the records into two columns.
/// </summary>
public sealed record RecordListLayout(double MaximumWidth, LayoutOptions HorizontalOptions, int Columns)
{
    public static RecordListLayout For(AdaptiveLayoutMode mode) => mode switch
    {
        AdaptiveLayoutMode.Compact => new(double.PositiveInfinity, LayoutOptions.Fill, 1),
        AdaptiveLayoutMode.Wide => new(1000, LayoutOptions.Center, 2),
        _ => new(640, LayoutOptions.Center, 1)
    };
}
