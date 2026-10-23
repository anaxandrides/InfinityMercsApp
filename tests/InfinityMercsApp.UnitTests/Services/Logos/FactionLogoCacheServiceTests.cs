using System.Net;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Logos;

public sealed class FactionLogoCacheServiceTests : IDisposable
{
    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    private FactionLogoCacheService Service(CountingLogoHandler handler) =>
        new(new HttpClient(handler), _records.Paths with { FactionLogoDirectory = Path.Combine(_records.Paths.CompaniesDirectory, "svg-cache") }, Substitute.For<IAppPackageFiles>());

    private static ArmyImportResume Unit(int id) => new() { Id = id, Logo = $"https://cdn.example/units/{id}.svg" };

    [Fact]
    public async Task CacheUnitLogosAsync_ManyMissing_DownloadsSeveralAtOnce()
    {
        var handler = new CountingLogoHandler();
        var logos = Service(handler);

        var result = await logos.CacheUnitLogosAsync(101, Enumerable.Range(1, 40).Select(Unit));

        Assert.Equal(40, result.Downloaded);
        Assert.True(handler.MostAtOnce > 1, $"At most {handler.MostAtOnce} download ran at once.");
        Assert.True(File.Exists(logos.GetCachedUnitLogoPath(101, 40)));
    }

    [Fact]
    public async Task CacheUnitLogosAsync_AlreadyOnDisk_IsReused()
    {
        var handler = new CountingLogoHandler();
        var logos = Service(handler);
        await logos.CacheUnitLogosAsync(101, [Unit(1)]);

        var result = await logos.CacheUnitLogosAsync(101, [Unit(1)]);

        Assert.Equal(1, result.CachedReuse);
        Assert.Equal(1, handler.Requests);
    }

    [Fact]
    public async Task CacheUnitLogosAsync_UnitListedTwice_DownloadsItOnce()
    {
        var handler = new CountingLogoHandler();

        await Service(handler).CacheUnitLogosAsync(101, [Unit(1), Unit(1), Unit(2)]);

        Assert.Equal(2, handler.Requests);
    }

    [Fact]
    public async Task CacheUnitLogosAsync_DownloadFails_LeavesNoFile()
    {
        var logos = Service(new CountingLogoHandler(HttpStatusCode.NotFound));

        var result = await logos.CacheUnitLogosAsync(101, [Unit(1)]);

        Assert.Equal(1, result.Failed);
        Assert.False(File.Exists(logos.GetCachedUnitLogoPath(101, 1)));
    }
}
