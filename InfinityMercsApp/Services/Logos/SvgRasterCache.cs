using System.Collections.Concurrent;
using SkiaSharp;
using Svg.Skia;

namespace InfinityMercsApp.Services.Logos;

/// <summary>
/// Disk + inflight-dedupe raster cache for logo SVGs. The core (this file) has no MAUI
/// dependency so it can be unit-tested against a plain temp directory; the platform wiring
/// (app data directory, display density, packaged-asset opener) lives in the partial in
/// <c>SvgRasterCache.Platform.cs</c>.
/// </summary>
public sealed partial class SvgRasterCache : ISvgRasterCache
{
    // Guard against pathological allocations from a bad logical size × density.
    private const int MaxPixelDimension = 1024;

    private readonly string _cacheRoot;
    private readonly string _rasterRoot;
    private readonly Func<double> _densityProvider;
    private readonly Func<string, CancellationToken, Task<Stream?>> _openPackagedAsync;

    // Dedupe concurrent builds by target PNG path so the same (icon, size) is never rasterized twice.
    private readonly ConcurrentDictionary<string, Task<string?>> _inflight =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Test/DI-friendly constructor. <paramref name="cacheRoot"/> is the <c>svg-cache</c> folder
    /// (rasters land in <c>{cacheRoot}/raster</c>); <paramref name="densityProvider"/> yields the
    /// display density; <paramref name="openPackagedAsync"/> opens a packaged (app-bundled) SVG by
    /// its logical name.
    /// </summary>
    public SvgRasterCache(
        string cacheRoot,
        Func<double> densityProvider,
        Func<string, CancellationToken, Task<Stream?>> openPackagedAsync)
    {
        _cacheRoot = Path.GetFullPath(cacheRoot);
        _rasterRoot = Path.Combine(_cacheRoot, "raster");
        _densityProvider = densityProvider;
        _openPackagedAsync = openPackagedAsync;
    }

    public async Task<string?> GetRasterPathAsync(
        string svgPath, double logicalWidth, double logicalHeight, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(svgPath) || logicalWidth <= 0 || logicalHeight <= 0)
        {
            return null;
        }

        var density = _densityProvider();
        if (density <= 0 || double.IsNaN(density) || double.IsInfinity(density))
        {
            density = 1;
        }

        var w = Math.Clamp((int)Math.Ceiling(logicalWidth * density), 1, MaxPixelDimension);
        var h = Math.Clamp((int)Math.Ceiling(logicalHeight * density), 1, MaxPixelDimension);

        var isPackaged = !Path.IsPathRooted(svgPath);
        var pngPath = BuildRasterPngPath(svgPath, isPackaged, w, h);

        // Cheap freshness check in addition to explicit Invalidate: a PNG newer than its source
        // SVG is reusable (packaged sources are immutable, so existence alone suffices).
        if (IsFresh(svgPath, pngPath, isPackaged))
        {
            return pngPath;
        }

        // The shared build task is not bound to this caller's token so one caller cancelling
        // cannot tear down a raster another caller is awaiting.
        var build = _inflight.GetOrAdd(pngPath, key => RasterizeAsync(svgPath, key, w, h, isPackaged));
        try
        {
            ct.ThrowIfCancellationRequested();
            return await build.ConfigureAwait(false);
        }
        finally
        {
            _inflight.TryRemove(new KeyValuePair<string, Task<string?>>(pngPath, build));
        }
    }

    public void Invalidate(string svgPath)
    {
        if (string.IsNullOrWhiteSpace(svgPath))
        {
            return;
        }

        try
        {
            var isPackaged = !Path.IsPathRooted(svgPath);
            var (dir, stem) = ResolveRasterLocation(svgPath, isPackaged);
            if (!Directory.Exists(dir))
            {
                return;
            }

            // The '@' delimiter keeps stems like "202" from matching "2020@…".
            foreach (var file in Directory.EnumerateFiles(dir, $"{stem}@*.png"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Best-effort; a locked/absent raster is not fatal.
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SvgRasterCache invalidate failed for '{svgPath}': {ex.Message}");
        }
    }

    private Task<string?> RasterizeAsync(string svgPath, string pngPath, int w, int h, bool isPackaged)
    {
        return Task.Run(async () =>
        {
            SKSvg? svg = null;
            try
            {
                // Another build may have completed between the fast-path check and here.
                if (IsFresh(svgPath, pngPath, isPackaged))
                {
                    return pngPath;
                }

                var source = isPackaged
                    ? await _openPackagedAsync(svgPath, CancellationToken.None).ConfigureAwait(false)
                    : File.OpenRead(svgPath);

                if (source is null)
                {
                    return null;
                }

                svg = new SKSvg();
                SKPicture? picture;
                await using (source)
                {
                    picture = svg.Load(source);
                }

                if (picture is null)
                {
                    return null;
                }

                var bounds = picture.CullRect;
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return null;
                }

                using var bitmap = new SKBitmap(w, h);
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);

                    // Fit-scale + centre, identical to the old per-widget PaintSurface handler.
                    var scale = Math.Min(w / bounds.Width, h / bounds.Height);
                    var x = (w - (bounds.Width * scale)) / 2f;
                    var y = (h - (bounds.Height * scale)) / 2f;

                    canvas.Translate(x, y);
                    canvas.Scale(scale);
                    canvas.DrawPicture(picture);
                }

                var directory = Path.GetDirectoryName(pngPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    // Write atomically so a reader never observes a half-written PNG.
                    var tmpPath = pngPath + ".tmp";
                    using (var fileStream = File.Create(tmpPath))
                    {
                        data.SaveTo(fileStream);
                    }

                    File.Move(tmpPath, pngPath, overwrite: true);
                }

                return pngPath;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"SvgRasterCache rasterize failed for '{svgPath}' -> '{pngPath}': {ex.Message}");
                return null;
            }
            finally
            {
                // Disposing the SKSvg disposes its picture, so this must run only after DrawPicture.
                svg?.Dispose();
            }
        });
    }

    private static bool IsFresh(string svgPath, string pngPath, bool isPackaged)
    {
        if (!File.Exists(pngPath))
        {
            return false;
        }

        if (isPackaged)
        {
            // Packaged SVGs ship inside the app bundle and never change under a given path.
            return true;
        }

        try
        {
            return File.GetLastWriteTimeUtc(pngPath) >= File.GetLastWriteTimeUtc(svgPath);
        }
        catch
        {
            return false;
        }
    }

    private string BuildRasterPngPath(string svgPath, bool isPackaged, int w, int h)
    {
        var (dir, stem) = ResolveRasterLocation(svgPath, isPackaged);
        return Path.Combine(dir, $"{stem}@{w}x{h}.png");
    }

    /// <summary>
    /// Maps a source SVG to the folder+stem its rasters live under. Cached sources mirror their
    /// sub-folder beneath <c>raster/</c> (so unit vs faction ids cannot collide); packaged sources
    /// key by a sanitized stem under <c>raster/packaged/</c>; anything else falls under
    /// <c>raster/external/</c>.
    /// </summary>
    private (string dir, string stem) ResolveRasterLocation(string svgPath, bool isPackaged)
    {
        if (isPackaged)
        {
            return (Path.Combine(_rasterRoot, "packaged"), SanitizeStem(StripExtension(svgPath)));
        }

        var full = Path.GetFullPath(svgPath);
        var stem = Path.GetFileNameWithoutExtension(full);

        var relative = Path.GetRelativePath(_cacheRoot, full);
        var underCacheRoot = !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
        if (underCacheRoot)
        {
            var relDir = Path.GetDirectoryName(relative);
            var dir = string.IsNullOrEmpty(relDir) ? _rasterRoot : Path.Combine(_rasterRoot, relDir);
            return (dir, stem);
        }

        return (Path.Combine(_rasterRoot, "external"), SanitizeStem(StripExtension(full)));
    }

    private static string StripExtension(string path)
    {
        var ext = Path.GetExtension(path);
        return string.IsNullOrEmpty(ext) ? path : path[..^ext.Length];
    }

    private static string SanitizeStem(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (c is '/' or '\\' or ':' || Array.IndexOf(invalid, c) >= 0)
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }
}
