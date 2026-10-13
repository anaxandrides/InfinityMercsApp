using System.Collections.Concurrent;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Draws an SVG scaled to fit and centred. <see cref="Source"/> is a packaged asset path
/// (e.g. <c>SVGCache/CBIcons/cube.svg</c>) or an absolute file path; each source is loaded once and shared.
/// </summary>
public class SvgIcon : SKCanvasView
{
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source), typeof(string), typeof(SvgIcon), propertyChanged: (bindable, _, _) => ((SvgIcon)bindable).Reload());

    private static readonly ConcurrentDictionary<string, Task<SKPicture?>> Pictures = new(StringComparer.Ordinal);

    private SKPicture? _picture;

    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private async void Reload()
    {
        var source = Source;
        _picture = null;
        InvalidateSurface();

        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        var picture = await Pictures.GetOrAdd(source, LoadAsync);
        if (source == Source)
        {
            _picture = picture;
            InvalidateSurface();
        }
    }

    private static async Task<SKPicture?> LoadAsync(string source)
    {
        try
        {
            await using var stream = Path.IsPathRooted(source)
                ? File.OpenRead(source)
                : await FileSystem.Current.OpenAppPackageFileAsync(source);
            return new SKSvg().Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SvgIcon failed to load '{source}': {ex.Message}");
            return null;
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var picture = _picture;
        var bounds = picture?.CullRect ?? SKRect.Empty;
        if (picture is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        canvas.Save();
        canvas.Translate(
            (e.Info.Width - bounds.Width * scale) / 2f - bounds.Left * scale,
            (e.Info.Height - bounds.Height * scale) / 2f - bounds.Top * scale);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }
}
