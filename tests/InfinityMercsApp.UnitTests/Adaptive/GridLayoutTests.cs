using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.UnitTests.Adaptive;

public sealed class GridLayoutTests
{
    [Fact]
    public void ColumnsAndRowsTakeXamlDefinitions()
    {
        var grid = new Grid();

        GridLayout.SetColumns(grid, "280,*,320");
        GridLayout.SetRows(grid, "Auto,*");

        Assert.Equal<GridLength>([new(280), GridLength.Star, new(320)], grid.ColumnDefinitions.Select(column => column.Width));
        Assert.Equal<GridLength>([GridLength.Auto, GridLength.Star], grid.RowDefinitions.Select(row => row.Height));
    }

    [Fact]
    public void ACellPlacesTheView()
    {
        var label = new Label();

        GridLayout.SetCell(label, new GridCell(1, 2, RowSpan: 3, ColumnSpan: 4));

        Assert.Equal(1, Grid.GetRow(label));
        Assert.Equal(2, Grid.GetColumn(label));
        Assert.Equal(3, Grid.GetRowSpan(label));
        Assert.Equal(4, Grid.GetColumnSpan(label));
    }
}
