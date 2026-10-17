using InfinityMercsApp.Presentation.Layouts;
namespace InfinityMercsApp.Views.Adaptive;

/// <summary>
/// Attached properties that let a ViewModel lay out a grid: <c>Cell</c> places a child, and
/// <c>Columns</c> / <c>Rows</c> take definitions in XAML's own form, such as "280,*,320" or "Auto,*".
/// </summary>
public static class GridLayout
{
    public static readonly BindableProperty CellProperty =
        BindableProperty.CreateAttached("Cell", typeof(GridCell), typeof(GridLayout), null, propertyChanged: OnCellChanged);

    public static readonly BindableProperty ColumnsProperty =
        BindableProperty.CreateAttached("Columns", typeof(string), typeof(GridLayout), null, propertyChanged: OnColumnsChanged);

    public static readonly BindableProperty RowsProperty =
        BindableProperty.CreateAttached("Rows", typeof(string), typeof(GridLayout), null, propertyChanged: OnRowsChanged);

    private static readonly ColumnDefinitionCollectionTypeConverter ColumnsConverter = new();
    private static readonly RowDefinitionCollectionTypeConverter RowsConverter = new();

    public static GridCell? GetCell(BindableObject view) => (GridCell?)view.GetValue(CellProperty);
    public static void SetCell(BindableObject view, GridCell? value) => view.SetValue(CellProperty, value);

    public static string? GetColumns(BindableObject grid) => (string?)grid.GetValue(ColumnsProperty);
    public static void SetColumns(BindableObject grid, string? value) => grid.SetValue(ColumnsProperty, value);

    public static string? GetRows(BindableObject grid) => (string?)grid.GetValue(RowsProperty);
    public static void SetRows(BindableObject grid, string? value) => grid.SetValue(RowsProperty, value);

    private static void OnCellChanged(BindableObject view, object? oldValue, object? newValue)
    {
        if (newValue is GridCell cell)
        {
            Grid.SetRow(view, cell.Row);
            Grid.SetColumn(view, cell.Column);
            Grid.SetRowSpan(view, cell.RowSpan);
            Grid.SetColumnSpan(view, cell.ColumnSpan);
        }
    }

    private static void OnColumnsChanged(BindableObject grid, object? oldValue, object? newValue)
    {
        if (grid is Grid target && newValue is string columns &&
            ColumnsConverter.ConvertFromInvariantString(columns) is ColumnDefinitionCollection definitions)
        {
            target.ColumnDefinitions = definitions;
        }
    }

    private static void OnRowsChanged(BindableObject grid, object? oldValue, object? newValue)
    {
        if (grid is Grid target && newValue is string rows &&
            RowsConverter.ConvertFromInvariantString(rows) is RowDefinitionCollection definitions)
        {
            target.RowDefinitions = definitions;
        }
    }
}
