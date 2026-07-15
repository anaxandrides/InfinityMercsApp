using InfinityMercsApp.Services.Logos;
using SkiaSharp;

namespace InfinityMercsApp.UnitTests;

public sealed class SvgRasterCacheTests : IDisposable
{
    private const string ValidSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\" viewBox=\"0 0 100 100\">" +
        "<rect x=\"0\" y=\"0\" width=\"100\" height=\"100\" fill=\"#ff0000\"/></svg>";

    private readonly string _cacheRoot;

    public SvgRasterCacheTests()
    {
        _cacheRoot = Path.Combine(Path.GetTempPath(), "svg-raster-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_cacheRoot, "units"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_cacheRoot))
            {
                Directory.Delete(_cacheRoot, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private SvgRasterCache CreateCache(double density = 1.0) =>
        new(_cacheRoot, () => density, (_, _) => Task.FromResult<Stream?>(null));

    private string WriteSvg(string relativePath, string content = ValidSvg)
    {
        var path = Path.Combine(_cacheRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task Rasterize_ProducesNonEmptyPngOfRequestedPixelSize()
    {
        var cache = CreateCache();
        var svgPath = WriteSvg(Path.Combine("units", "202-15.svg"));

        var pngPath = await cache.GetRasterPathAsync(svgPath, 48, 48);

        Assert.NotNull(pngPath);
        Assert.True(File.Exists(pngPath));
        Assert.True(new FileInfo(pngPath!).Length > 0);

        using var decoded = SKBitmap.Decode(pngPath);
        Assert.NotNull(decoded);
        Assert.Equal(48, decoded!.Width);
        Assert.Equal(48, decoded.Height);
    }

    [Fact]
    public async Task Rasterize_HonoursDensityWhenComputingPixelSize()
    {
        var cache = CreateCache(density: 2.0);
        var svgPath = WriteSvg(Path.Combine("units", "202-15.svg"));

        var pngPath = await cache.GetRasterPathAsync(svgPath, 48, 48);

        Assert.NotNull(pngPath);
        using var decoded = SKBitmap.Decode(pngPath);
        Assert.Equal(96, decoded!.Width);
        Assert.Equal(96, decoded.Height);
    }

    [Fact]
    public async Task SecondCall_SameSize_ReturnsSamePathWithoutReEncoding()
    {
        var cache = CreateCache();
        var svgPath = WriteSvg(Path.Combine("units", "202-15.svg"));

        var first = await cache.GetRasterPathAsync(svgPath, 48, 48);
        Assert.NotNull(first);
        var firstWrite = File.GetLastWriteTimeUtc(first!);

        var second = await cache.GetRasterPathAsync(svgPath, 48, 48);

        Assert.Equal(first, second);
        Assert.Equal(firstWrite, File.GetLastWriteTimeUtc(second!));
    }

    [Fact]
    public async Task Invalidate_DeletesPng_AndNextCallRegeneratesIt()
    {
        var cache = CreateCache();
        var svgPath = WriteSvg(Path.Combine("units", "202-15.svg"));

        var pngPath = await cache.GetRasterPathAsync(svgPath, 48, 48);
        Assert.True(File.Exists(pngPath));

        cache.Invalidate(svgPath);
        Assert.False(File.Exists(pngPath));

        var regenerated = await cache.GetRasterPathAsync(svgPath, 48, 48);
        Assert.Equal(pngPath, regenerated);
        Assert.True(File.Exists(regenerated));
    }

    [Fact]
    public async Task MissingSvg_ReturnsNull_DoesNotThrow()
    {
        var cache = CreateCache();
        var missingPath = Path.Combine(_cacheRoot, "units", "does-not-exist.svg");

        var pngPath = await cache.GetRasterPathAsync(missingPath, 48, 48);

        Assert.Null(pngPath);
    }

    [Fact]
    public async Task GarbageSvg_ReturnsNull_DoesNotThrow()
    {
        var cache = CreateCache();
        var svgPath = WriteSvg(Path.Combine("units", "garbage.svg"), "this is not svg content");

        var pngPath = await cache.GetRasterPathAsync(svgPath, 48, 48);

        Assert.Null(pngPath);
    }
}
