using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Draws a column of SVG icons, centred: one column for up to three, two columns of three beyond that.
/// Each <see cref="Sources"/> entry is a packaged asset path or an absolute file path.
/// </summary>
public class SvgIconColumn : SKCanvasView
{
    private const float MaxIconSize = 24f;
    private const float VerticalGap = 5f;
    private const float HorizontalGap = 6f;
    private const int RowsPerColumn = 3;

    public static readonly BindableProperty SourcesProperty = BindableProperty.Create(
        nameof(Sources), typeof(IReadOnlyList<string>), typeof(SvgIconColumn),
        propertyChanged: (bindable, _, _) => ((SvgIconColumn)bindable).Reload());

    private IReadOnlyList<SKPicture> _pictures = [];

    public IReadOnlyList<string>? Sources
    {
        get => (IReadOnlyList<string>?)GetValue(SourcesProperty);
        set => SetValue(SourcesProperty, value);
    }

    private async void Reload()
    {
        var sources = Sources;
        var pictures = new List<SKPicture>();
        foreach (var source in sources ?? [])
        {
            if (await SvgPictureCache.GetAsync(source) is { } picture)
            {
                pictures.Add(picture);
            }
        }

        if (ReferenceEquals(sources, Sources))
        {
            _pictures = pictures;
            InvalidateSurface();
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var pictures = _pictures;
        if (pictures.Count == 0)
        {
            return;
        }

        if (pictures.Count <= RowsPerColumn)
        {
            var size = IconSize(pictures.Count, e.Info.Height, e.Info.Width);
            DrawColumn(canvas, pictures, (e.Info.Width - size) / 2f, size, e.Info.Height);
            return;
        }

        var leftCount = RowsPerColumn;
        var rightCount = pictures.Count - leftCount;
        var twoColumnSize = IconSize(Math.Max(leftCount, rightCount), e.Info.Height, (e.Info.Width - HorizontalGap) / 2f);
        var leftX = Math.Max(0f, (e.Info.Width - ((twoColumnSize * 2f) + HorizontalGap)) / 2f);
        DrawColumn(canvas, pictures.Take(leftCount).ToList(), leftX, twoColumnSize, e.Info.Height);
        DrawColumn(canvas, pictures.Skip(leftCount).ToList(), leftX + twoColumnSize + HorizontalGap, twoColumnSize, e.Info.Height);
    }

    // As large as fits: no more than MaxIconSize, the column's share of the height, or the width.
    private static float IconSize(int rows, float height, float width) =>
        Math.Max(1f, Math.Min(MaxIconSize, Math.Min((height - ((rows - 1) * VerticalGap)) / rows, width)));

    // One column of icons at x, centred vertically.
    private static void DrawColumn(SKCanvas canvas, IReadOnlyList<SKPicture> pictures, float x, float size, float height)
    {
        var columnHeight = (pictures.Count * size) + ((pictures.Count - 1) * VerticalGap);
        var startY = Math.Max(0f, (height - columnHeight) / 2f);
        for (var i = 0; i < pictures.Count; i++)
        {
            var y = startY + (i * (size + VerticalGap));
            SkiaDrawing.DrawPictureInRect(canvas, pictures[i], new SKRect(x, y, x + size, y + size));
        }
    }
}
