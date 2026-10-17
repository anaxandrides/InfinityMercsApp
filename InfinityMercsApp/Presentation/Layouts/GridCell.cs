namespace InfinityMercsApp.Presentation.Layouts;

/// <summary>Where a view sits in a grid, for ViewModels that lay out their page per width.</summary>
public sealed record GridCell(int Row, int Column, int RowSpan = 1, int ColumnSpan = 1);
