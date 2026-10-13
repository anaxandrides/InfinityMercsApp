using System.Collections.Concurrent;
using SkiaSharp;
using Svg.Skia;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Loads each SVG once and shares the picture. A source is a packaged asset path
/// (e.g. <c>SVGCache/CBIcons/cube.svg</c>) or an absolute file path. Shared pictures must not be disposed.
/// </summary>
public static class SvgPictureCache
{
    private static readonly ConcurrentDictionary<string, Task<SKPicture?>> Pictures = new(StringComparer.Ordinal);

    /// <summary>The picture, or null when the source is blank or cannot be loaded.</summary>
    public static Task<SKPicture?> GetAsync(string? source) =>
        string.IsNullOrWhiteSpace(source) ? Task.FromResult<SKPicture?>(null) : Pictures.GetOrAdd(source, LoadAsync);

    // Packaged asset names are case-sensitive on Android, so a lower-case spelling is tried as well.
    private static async Task<SKPicture?> LoadAsync(string source)
    {
        if (Path.IsPathRooted(source))
        {
            return await TryLoadAsync(source, () => Task.FromResult<Stream>(File.OpenRead(source)));
        }

        return await TryLoadAsync(source, () => FileSystem.Current.OpenAppPackageFileAsync(source))
            ?? (source == source.ToLowerInvariant()
                ? null
                : await TryLoadAsync(source, () => FileSystem.Current.OpenAppPackageFileAsync(source.ToLowerInvariant())));
    }

    private static async Task<SKPicture?> TryLoadAsync(string source, Func<Task<Stream>> open)
    {
        try
        {
            await using var stream = await open();
            return new SKSvg().Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load SVG '{source}': {ex.Message}");
            return null;
        }
    }
}
