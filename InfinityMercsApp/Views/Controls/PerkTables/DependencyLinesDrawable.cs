namespace InfinityMercsApp.Views.Controls.PerkTables;

/// <summary>Draws the lines between perks that depend on each other, over the table's cells.</summary>
public sealed class DependencyLinesDrawable : IDrawable
{
    private static readonly Color LineColor = Color.FromArgb("#34D399");

    public IReadOnlyList<LineSegment> Lines { get; set; } = [];

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 2f;
        foreach (var line in Lines)
        {
            canvas.DrawLine((float)line.X1, (float)line.Y1, (float)line.X2, (float)line.Y2);
        }
    }
}
