using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>
/// Where the marketplace page puts things at each width. Compact and medium stack the store info above the
/// sections; expanded puts the store info in a rail beside them, and wide adds the cart rail and buying. All but
/// compact show the sections in two columns: items on the left, troop types above augments on the right.
/// </summary>
public sealed record MarketplacesLayout(
    string ContentColumns,
    string ContentRows,
    double ContentColumnSpacing,
    double ContentRowSpacing,
    GridCell PlaceholderCell,
    GridCell StoreInfoCell,
    GridCell SectionsCell,
    GridCell CartCell,
    string SectionsColumns,
    string SectionsRows,
    GridCell ItemsCell,
    GridCell TroopTypesCell,
    GridCell AugmentsCell,
    bool OffersBuying)
{
    public static MarketplacesLayout For(AdaptiveLayoutMode mode)
    {
        var stacked = mode is AdaptiveLayoutMode.Compact or AdaptiveLayoutMode.Medium;
        var wide = mode == AdaptiveLayoutMode.Wide;
        var oneSectionColumn = mode == AdaptiveLayoutMode.Compact;

        return new MarketplacesLayout(
            ContentColumns: stacked ? "*" : wide ? "280,*,320" : "280,*",
            ContentRows: stacked ? "Auto,*" : "*",
            ContentColumnSpacing: stacked ? 0 : 12,
            ContentRowSpacing: stacked ? 12 : 0,
            PlaceholderCell: stacked ? new(0, 0, RowSpan: 2) : new(0, 0, ColumnSpan: wide ? 3 : 2),
            StoreInfoCell: new(0, 0),
            SectionsCell: stacked ? new(1, 0) : new(0, 1),
            CartCell: new(0, 2),
            SectionsColumns: oneSectionColumn ? "*" : "*,*",
            SectionsRows: oneSectionColumn ? "Auto,Auto,Auto" : "Auto,Auto",
            ItemsCell: oneSectionColumn ? new(0, 0) : new(0, 0, RowSpan: 2),
            TroopTypesCell: oneSectionColumn ? new(1, 0) : new(0, 1),
            AugmentsCell: oneSectionColumn ? new(2, 0) : new(1, 1),
            OffersBuying: wide);
    }
}
