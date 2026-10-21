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
    public static async Task<SKPicture?> GetAsync(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var picture = await Pictures.GetOrAdd(source, LoadAsync);
        if (picture is null)
        {
            // Forget failures so a logo downloaded later is picked up on the next request.
            Pictures.TryRemove(source, out _);
        }

        return picture;
    }

    // Packaged asset names are case-sensitive on Android, so a lower-case spelling is tried as well.
    private static async Task<SKPicture?> LoadAsync(string source)
    {
        if (Path.IsPathRooted(source))
        {
            // A logo not downloaded yet is expected; the caller tries its next source.
            return File.Exists(source)
                ? await TryLoadAsync(source, () => Task.FromResult<Stream>(File.OpenRead(source)))
                : null;
        }

        return await TryLoadAsync(source, () => FileSystem.Current.OpenAppPackageFileAsync(source))
            ?? (source == source.ToLowerInvariant()
                ? null
                : await TryLoadAsync(source, () => FileSystem.Current.OpenAppPackageFileAsync(source.ToLowerInvariant())));
    }

    // Parsing is synchronous CPU work, so it runs off the UI thread; a whole list of logos loading at once
    // would otherwise hold up the list. An SKPicture is immutable once built, so it can be drawn anywhere.
    private static async Task<SKPicture?> TryLoadAsync(string source, Func<Task<Stream>> open)
    {
        try
        {
            await using var stream = await open();
            return await Task.Run(() => new SKSvg().Load(stream));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load SVG '{source}': {ex.Message}");
            return null;
        }
    }
}
