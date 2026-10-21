using System.Collections.Concurrent;
using SkiaSharp;

namespace InfinityMercsApp.Views.Fonts;

/// <summary>
/// How wide text is in one of the app's fonts, read from the font file itself. Windows can measure a label before
/// its bundled font has loaded, getting the fallback font's width; this width is the same on every platform.
/// </summary>
public static class FontTextWidth
{
    private const string ResourcePrefix = "Fonts/";

    private static readonly ConcurrentDictionary<string, SKTypeface?> Typefaces = new(StringComparer.Ordinal);

    /// <summary>
    /// The width of <paramref name="text"/> at <paramref name="fontSize"/>, or null when it is blank or
    /// <paramref name="fontFamily"/> is not one of <see cref="AppFonts.All"/>.
    /// </summary>
    public static double? Of(string? text, string? fontFamily, double fontSize, bool bold)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(fontFamily) || fontSize <= 0 ||
            Typefaces.GetOrAdd(fontFamily, Load) is not { } typeface)
        {
            return null;
        }

        using var font = new SKFont(typeface, (float)fontSize) { Embolden = bold };
        return font.MeasureText(text);
    }

    private static SKTypeface? Load(string fontFamily)
    {
        if (AppFonts.All.FirstOrDefault(font => font.Alias == fontFamily) is not { } appFont)
        {
            return null;
        }

        using var stream = typeof(FontTextWidth).Assembly.GetManifestResourceStream(ResourcePrefix + appFont.FileName);
        if (stream is null)
        {
            return null;
        }

        using var data = SKData.Create(stream);
        return SKTypeface.FromData(data);
    }
}
