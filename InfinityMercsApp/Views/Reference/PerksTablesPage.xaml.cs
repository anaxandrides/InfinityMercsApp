using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.ViewModels.Reference;
using InfinityMercsApp.Views.Adaptive;
using InfinityMercsApp.Views.Controls.PerkTables;

namespace InfinityMercsApp.Views.Reference;

/// <summary>
/// The perk tables. The selected table comes from <see cref="PerksTablesViewModel.Table"/> and is drawn by
/// <see cref="PerkTableView"/>; the page only arranges the legend and the table for the window's width.
/// </summary>
public partial class PerksTablesPage : AdaptiveContentPage
{
    public PerksTablesPage(PerksTablesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        ApplyLayout();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    // Compact and medium windows show the legend above a table that pans; wider ones put it in a rail beside a table
    // that fits.
    private void ApplyLayout()
    {
        InlineLegend.IsVisible = IsCompact || IsMedium;
        LegendRail.IsVisible = IsExpandedOrWider;
        TableViewport.FitsViewport = IsExpandedOrWider;

        if (IsExpandedOrWider)
        {
            BodyGrid.ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(IsWide ? 300d : 240d)),
                new ColumnDefinition(GridLength.Star)
            ];
            Grid.SetColumn(LegendRail, 0);
            Grid.SetColumn(TableViewport, 1);
        }
        else
        {
            BodyGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            Grid.SetColumn(TableViewport, 0);
        }
    }
}
