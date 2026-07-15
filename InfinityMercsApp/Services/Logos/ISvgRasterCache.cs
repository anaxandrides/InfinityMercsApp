namespace InfinityMercsApp.Services.Logos;

/// <summary>
/// Rasterizes logo SVGs into cached PNGs (one per source + pixel size) so item templates can
/// bind a native <c>Image</c> to a file instead of hosting a live Skia surface per widget.
/// </summary>
public interface ISvgRasterCache
{
    /// <summary>
    /// Returns a PNG file path rendered from <paramref name="svgPath"/> at the given logical
    /// size, or <c>null</c> if the SVG is missing/unparseable. Safe to call from the UI thread;
    /// rasterization runs off-thread.
    /// </summary>
    Task<string?> GetRasterPathAsync(
        string svgPath, double logicalWidth, double logicalHeight,
        CancellationToken ct = default);

    /// <summary>
    /// Delete all rasters derived from <paramref name="svgPath"/>. Call whenever the SVG is
    /// (re)written so a reader never observes a raster of stale artwork.
    /// </summary>
    void Invalidate(string svgPath);
}
