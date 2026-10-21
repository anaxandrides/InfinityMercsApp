using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views.Controls;

/// <summary>Draws SVG pictures scaled to fit, centred, for the controls that paint them on a Skia canvas.</summary>
internal static class SkiaDrawing
{
    /// <summary>Draws <paramref name="picture"/> as large as fits inside <paramref name="destination"/>, centred.</summary>
    public static void DrawPictureInRect(SKCanvas canvas, SKPicture picture, SKRect destination)
    {
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0 || destination.Width <= 0 || destination.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(destination.Width / bounds.Width, destination.Height / bounds.Height);
        var scaledWidth = bounds.Width * scale;
        var scaledHeight = bounds.Height * scale;
        var left = destination.Left + ((destination.Width - scaledWidth) / 2f);
        var top = destination.Top + ((destination.Height - scaledHeight) / 2f);

        canvas.Save();
        canvas.Translate(left - (bounds.Left * scale), top - (bounds.Top * scale));
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }

    /// <summary>Clears the surface and draws <paramref name="picture"/> to fit it; nothing more for no picture.</summary>
    public static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (picture is null)
        {
            return;
        }

        var destination = new SKRect(0, 0, e.Info.Width, e.Info.Height);
        DrawPictureInRect(canvas, picture, destination);
    }
}
