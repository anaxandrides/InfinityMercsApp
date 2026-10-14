using System.ComponentModel;
using InfinityMercsApp.Domain.Models.Perks;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

/// <summary>
/// Draws the selected perk table. The table's contents come from <see cref="PerksTablesViewModel.Table"/>;
/// the grid is built here because its rows and columns vary per list, and the dependency lines
/// are drawn over the cells once they have been measured.
/// </summary>
public partial class PerksTablesPage : AdaptiveContentPage
{
    private const double HeaderRowHeight = 56;
    private const double RollColumnWidth = 110;

    private readonly PerksTablesViewModel _viewModel;
    private readonly Dictionary<PerkPosition, Border> _perkCellByTrackTier = [];
    private readonly DependencyLinesDrawable _dependencyLinesDrawable = new();

    // When true (compact / medium) the table keeps a readable minimum size and pans inside the
    // viewport instead of squishing to fit.
    private bool _panTable = true;
    private double _minTableWidth;
    private double _minTableHeight;

    public PerksTablesPage(PerksTablesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        DependencyLinesView.Drawable = _dependencyLinesDrawable;
        PerksTableGrid.SizeChanged += OnTableSizeChanged;
        DependencyLinesView.SizeChanged += OnTableSizeChanged;
        PerksGridHost.SizeChanged += OnTableSizeChanged;
        TableViewport.SizeChanged += OnTableViewportSizeChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        BuildPerksTableGrid();
        ApplyLayout();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerksTablesViewModel.Table))
        {
            BuildPerksTableGrid();
            ScheduleOverlayRefresh();
        }
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void ApplyLayout()
    {
        _panTable = IsCompact || IsMedium;
        InlineLegend.IsVisible = _panTable;
        LegendRail.IsVisible = IsExpandedOrWider;

        if (IsExpandedOrWider)
        {
            var railWidth = IsWide ? 300d : 240d;
            BodyGrid.ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(railWidth)),
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

        ApplyResponsiveGridSize();
        ScheduleOverlayRefresh();
    }

    private void BuildPerksTableGrid()
    {
        _perkCellByTrackTier.Clear();
        _minTableWidth = 0;
        _minTableHeight = 0;
        PerksTableGrid.Children.Clear();
        PerksTableGrid.RowDefinitions.Clear();
        PerksTableGrid.ColumnDefinitions.Clear();

        var table = _viewModel.Table;
        if (table is null)
        {
            UpdateLineOverlay();
            return;
        }

        PerksTableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = RollColumnWidth });
        PerksTableGrid.RowDefinitions.Add(new RowDefinition { Height = HeaderRowHeight });
        AddCell(BuildHeaderCell("Roll"), 0, 0);
        for (var tier = 1; tier <= table.TierCount; tier++)
        {
            PerksTableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            AddCell(BuildHeaderCell($"Tier {tier}"), 0, tier);
        }

        for (var row = 1; row <= table.Tracks.Count; row++)
        {
            var track = table.Tracks[row - 1];
            PerksTableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            AddCell(BuildTierCell(track.RollLabel), row, 0);

            for (var tier = 1; tier <= table.TierCount; tier++)
            {
                var cell = BuildPerkCell(track.TierTexts[tier - 1]);
                AddCell(cell, row, tier);
                _perkCellByTrackTier[new PerkPosition(track.TrackNumber, tier)] = cell;
            }
        }

        // Keep the table readable when it must pan (compact / medium): a wide perk tree should
        // scroll horizontally rather than crush every tier column.
        _minTableWidth = RollColumnWidth + (table.TierCount * 120d);
        _minTableHeight = HeaderRowHeight + (table.Tracks.Count * 64d);

        ApplyResponsiveGridSize();
        UpdateLineOverlay();
        ScheduleOverlayRefresh();
    }

    private void AddCell(View cell, int row, int column)
    {
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, column);
        PerksTableGrid.Children.Add(cell);
    }

    private static Border BuildHeaderCell(string text)
    {
        return new Border
        {
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#161B22"),
            Padding = 8,
            Content = new Label
            {
                Text = text,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = Color.FromArgb("#E6EBF2")
            }
        };
    }

    private static Border BuildTierCell(string text)
    {
        return new Border
        {
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#0E1116"),
            Padding = new Thickness(8, 10),
            MinimumWidthRequest = 70,
            Content = new Label
            {
                Text = text,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = Color.FromArgb("#E6EBF2")
            }
        };
    }

    private static Border BuildPerkCell(string text)
    {
        var hasText = !string.IsNullOrWhiteSpace(text);
        return new Border
        {
            StrokeThickness = 1,
            BackgroundColor = hasText
                ? Color.FromArgb("#3334D399")
                : Color.FromArgb("#161B22"),
            Padding = new Thickness(10, 8),
            HorizontalOptions = LayoutOptions.Fill,
            Content = new Label
            {
                Text = text,
                Style = (Style)Application.Current!.Resources["LabelBody"],
                LineBreakMode = LineBreakMode.WordWrap,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Color.FromArgb("#E6EBF2")
            }
        };
    }

    private void OnTableSizeChanged(object? sender, EventArgs e)
    {
        UpdateLineOverlay();
        ScheduleOverlayRefresh();
    }

    private void OnTableViewportSizeChanged(object? sender, EventArgs e)
    {
        ApplyResponsiveGridSize();
        UpdateLineOverlay();
        ScheduleOverlayRefresh();
    }

    private void ApplyResponsiveGridSize()
    {
        var viewportWidth = TableViewport.Width;
        var viewportHeight = TableViewport.Height;
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return;
        }

        // Expanded / wide: fit the viewport so star rows/columns reflow. Compact / medium: never
        // shrink below the readable minimum, letting the Both-orientation ScrollView pan instead.
        var targetWidth = _panTable ? Math.Max(viewportWidth, _minTableWidth) : viewportWidth;
        var targetHeight = _panTable ? Math.Max(viewportHeight, _minTableHeight) : viewportHeight;

        PerksGridHost.WidthRequest = targetWidth;
        PerksGridHost.HeightRequest = targetHeight;
        PerksTableGrid.WidthRequest = targetWidth;
        PerksTableGrid.HeightRequest = targetHeight;
        DependencyLinesView.WidthRequest = targetWidth;
        DependencyLinesView.HeightRequest = targetHeight;
    }

    private void ScheduleOverlayRefresh()
    {
        if (Dispatcher is null)
        {
            return;
        }

        Dispatcher.Dispatch(UpdateLineOverlay);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), UpdateLineOverlay);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), UpdateLineOverlay);
    }

    private void UpdateLineOverlay()
    {
        if (PerksTableGrid.Width <= 0 || PerksTableGrid.Height <= 0)
        {
            return;
        }

        ApplyResponsiveGridSize();

        var lines = new List<LineSegment>();
        foreach (var link in _viewModel.Table?.Links ?? [])
        {
            if (!_perkCellByTrackTier.TryGetValue(new PerkPosition(link.Track, link.FromTier), out var fromCell))
            {
                continue;
            }

            if (!_perkCellByTrackTier.TryGetValue(new PerkPosition(link.Track, link.ToTier), out var toCell))
            {
                continue;
            }

            if (fromCell.Width <= 0 || fromCell.Height <= 0 || toCell.Width <= 0 || toCell.Height <= 0)
            {
                continue;
            }

            var parentLeft = fromCell.X;
            var parentRight = fromCell.X + fromCell.Width;
            var childLeft = toCell.X;
            var childRight = toCell.X + toCell.Width;
            var startY = fromCell.Y + (fromCell.Height / 2d);
            var endY = toCell.Y + (toCell.Height / 2d);

            const double inset = 20d;
            double startX;
            double endX;
            if (childLeft >= parentRight)
            {
                // Parent is left of child: draw near each edge with 20px inset.
                startX = parentRight - inset;
                endX = childLeft + inset;
            }
            else if (parentLeft >= childRight)
            {
                // Parent is right of child: mirrored case.
                startX = parentLeft + inset;
                endX = childRight - inset;
            }
            else
            {
                // Overlap fallback.
                startX = fromCell.X + (fromCell.Width / 2d);
                endX = toCell.X + (toCell.Width / 2d);
            }

            lines.Add(new LineSegment(startX, startY, endX, endY));
        }

        _dependencyLinesDrawable.SetLines(lines);
        DependencyLinesView.Invalidate();
    }

    private readonly record struct LineSegment(double X1, double Y1, double X2, double Y2);

    private sealed class DependencyLinesDrawable : IDrawable
    {
        private IReadOnlyList<LineSegment> _lines = [];

        public void SetLines(IReadOnlyList<LineSegment> lines)
        {
            _lines = lines;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Color.FromArgb("#34D399");
            canvas.StrokeSize = 2f;

            foreach (var line in _lines)
            {
                canvas.DrawLine((float)line.X1, (float)line.Y1, (float)line.X2, (float)line.Y2);
            }
        }
    }
}
