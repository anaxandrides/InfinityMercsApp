using InfinityMercsApp.Domain.Models.Perks.Tables;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Controls.PerkTables;

/// <summary>
/// A perk table's cells: the roll column, a column per tier and a row per track. Each time it is arranged, its cells
/// have their final places, so the dependency lines are worked out then, with no events or delayed redraws.
/// </summary>
public sealed class PerkCellsGrid : Grid
{
    public const double HeaderRowHeight = 56;
    public const double RollColumnWidth = 110;

    // Below these a panning table stops shrinking, so a wide tree scrolls rather than crushing its columns.
    private const double MinimumTierWidth = 120;
    private const double MinimumTrackHeight = 64;

    private readonly Dictionary<PerkPosition, Border> _perkCells = [];
    private readonly GraphicsView _lines;
    private readonly DependencyLinesDrawable _drawable = new();
    private IReadOnlyList<PerkTableLink> _links = [];

    public PerkCellsGrid(GraphicsView lines)
    {
        _lines = lines;
        _lines.Drawable = _drawable;
        RowSpacing = 2;
        ColumnSpacing = 2;
    }

    /// <summary>The smallest size the table stays readable at.</summary>
    public Size MinimumTableSize { get; private set; }

    /// <summary>Lays out <paramref name="table"/>'s cells; none for no table.</summary>
    public void Show(PerkTable? table)
    {
        _perkCells.Clear();
        Children.Clear();
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        _links = table?.Links ?? [];
        MinimumTableSize = Size.Zero;
        if (table is null)
        {
            return;
        }

        ColumnDefinitions.Add(new ColumnDefinition { Width = RollColumnWidth });
        RowDefinitions.Add(new RowDefinition { Height = HeaderRowHeight });
        AddCell(HeaderCell("Roll"), 0, 0);
        for (var tier = 1; tier <= table.TierCount; tier++)
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            AddCell(HeaderCell($"Tier {tier}"), 0, tier);
        }

        for (var row = 1; row <= table.Tracks.Count; row++)
        {
            var track = table.Tracks[row - 1];
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            AddCell(RollCell(track.RollLabel), row, 0);
            for (var tier = 1; tier <= table.TierCount; tier++)
            {
                var cell = PerkCell(track.TierTexts[tier - 1]);
                AddCell(cell, row, tier);
                _perkCells[new PerkPosition(track.TrackNumber, tier)] = cell;
            }
        }

        MinimumTableSize = new Size(
            RollColumnWidth + (table.TierCount * MinimumTierWidth),
            HeaderRowHeight + (table.Tracks.Count * MinimumTrackHeight));
    }

    protected override Size ArrangeOverride(Rect bounds)
    {
        var size = base.ArrangeOverride(bounds);
        _drawable.Lines = DependencyLines();
        _lines.Invalidate();
        return size;
    }

    // A line from each perk to the next one in its track that depends on it, between the cells' facing edges.
    private List<LineSegment> DependencyLines()
    {
        const double inset = 20d;
        var lines = new List<LineSegment>();
        foreach (var link in _links)
        {
            if (!_perkCells.TryGetValue(new PerkPosition(link.Track, link.FromTier), out var from) ||
                !_perkCells.TryGetValue(new PerkPosition(link.Track, link.ToTier), out var to) ||
                from.Width <= 0 || from.Height <= 0 || to.Width <= 0 || to.Height <= 0)
            {
                continue;
            }

            var startY = from.Y + (from.Height / 2d);
            var endY = to.Y + (to.Height / 2d);
            if (to.X >= from.X + from.Width)
            {
                lines.Add(new LineSegment(from.X + from.Width - inset, startY, to.X + inset, endY));
            }
            else if (from.X >= to.X + to.Width)
            {
                lines.Add(new LineSegment(from.X + inset, startY, to.X + to.Width - inset, endY));
            }
            else
            {
                lines.Add(new LineSegment(from.X + (from.Width / 2d), startY, to.X + (to.Width / 2d), endY));
            }
        }

        return lines;
    }

    private void AddCell(View cell, int row, int column) => this.Add(cell, column, row);

    private static Border HeaderCell(string text) => new()
    {
        StrokeThickness = 1,
        BackgroundColor = Color.FromArgb("#161B22"),
        Padding = 8,
        Content = CentredLabel(text, bold: true)
    };

    private static Border RollCell(string text) => new()
    {
        StrokeThickness = 1,
        BackgroundColor = Color.FromArgb("#0E1116"),
        Padding = new Thickness(8, 10),
        MinimumWidthRequest = 70,
        Content = CentredLabel(text, bold: true)
    };

    private static Border PerkCell(string text)
    {
        var label = CentredLabel(text, bold: false);
        label.Style = AppStyles.Named("LabelBody");
        label.LineBreakMode = LineBreakMode.WordWrap;
        return new Border
        {
            StrokeThickness = 1,
            BackgroundColor = string.IsNullOrWhiteSpace(text) ? Color.FromArgb("#161B22") : Color.FromArgb("#3334D399"),
            Padding = new Thickness(10, 8),
            HorizontalOptions = LayoutOptions.Fill,
            Content = label
        };
    }

    private static Label CentredLabel(string text, bool bold) => new()
    {
        Text = text,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        TextColor = Color.FromArgb("#E6EBF2")
    };
}
