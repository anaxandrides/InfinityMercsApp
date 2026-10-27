using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;
using MetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.UnitTests.Services.Logos;

/// <summary>Where each logo comes from: the cache, the app's package, the network, or the copy bundled as a stand-in.</summary>
public sealed class FactionLogoRulesTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly ILogoStore _store = Substitute.For<ILogoStore>();
    private readonly IAppPackageFiles _package = Substitute.For<IAppPackageFiles>();
    private readonly FactionLogoCacheService _logos;

    public FactionLogoRulesTests() => _logos = new FactionLogoCacheService(_store, _records.Paths, _package);

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task CacheAllAsync_DownloadFails_UsesTheBundledCopy()
    {
        _package.ExistsAsync("SVGCache/factions/1199.svg").Returns(true);
        _store.CopyPackagedAsync("SVGCache/factions/1199.svg", _logos.GetCachedLogoPath(1199), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _logos.CacheAllAsync([Faction(1199, "https://cdn.example/1199.svg")]);

        Assert.Equal(1, result.Downloaded);
    }

    [Fact]
    public async Task CacheAllAsync_DownloadFailsAndNothingBundled_Fails()
    {
        var result = await _logos.CacheFactionLogosFromRecordsAsync([Faction(1199, "https://cdn.example/1199.svg")]);

        Assert.Equal(1, result.Failed);
        await _store.DidNotReceive().CopyPackagedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CacheAllAsync_PackageUnreadable_Fails()
    {
        _package.ExistsAsync(Arg.Any<string>()).Returns<bool>(_ => throw new IOException("No package."));

        Assert.Equal(1, (await _logos.CacheAllAsync([Faction(1199, "https://cdn.example/1199.svg")])).Failed);
    }

    [Fact]
    public async Task CacheAllAsync_PackagedLogo_IsCopied()
    {
        _store.CopyPackagedAsync("SVGCache/factions/101.svg", _logos.GetCachedLogoPath(101), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _logos.CacheAllAsync([Faction(101, "SVGCache/factions/101.svg"), Faction(102, "SVGCache/factions/102.svg")]);

        Assert.Equal(1, result.Downloaded);
        Assert.Equal(1, result.Failed);
        await _store.DidNotReceive().FetchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ftp://cdn.example/101.svg")]
    [InlineData("not a url")]
    public async Task CacheAllAsync_NoUsableUrl_FailsWithoutFetching(string? url)
    {
        Assert.Equal(1, (await _logos.CacheAllAsync([Faction(101, url)])).Failed);
        await _store.DidNotReceive().FetchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CacheAllAsync_FactionWithoutAnId_CountsAsMissing()
    {
        var result = await _logos.CacheAllAsync([Faction(0, "https://cdn.example/0.svg")]);

        Assert.Equal(1, result.TotalFactions);
        Assert.Equal(1, result.MissingLogoUrl);
    }

    [Fact]
    public async Task CacheUnitLogosFromRecordsAsync_Cached_IsReused()
    {
        _store.Has(_logos.GetCachedUnitLogoPath(101, 5)).Returns(true);

        var result = await _logos.CacheUnitLogosFromRecordsAsync(101, [new Resume { UnitId = 5, Logo = "https://cdn.example/5.svg" }, new Resume { UnitId = 0 }]);

        Assert.Equal(1, result.CachedReuse);
        Assert.Equal(1, result.MissingLogoUrl);
    }

    [Fact]
    public async Task CacheUnitLogosFromRecordsAsync_TagCompanyUnit_IsAlwaysFetchedAgain()
    {
        var path = _logos.GetCachedUnitLogoPath(2003, 1);
        _store.Has(path).Returns(true);
        _store.FetchAsync("https://cdn.example/tag.svg", path, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _logos.CacheUnitLogosFromRecordsAsync(2003, [new Resume { UnitId = 1, Logo = "https://cdn.example/tag.svg" }]);

        Assert.Equal(1, result.Downloaded);
    }

    [Fact]
    public void TryGetCachedPaths_Cached_ReturnThem()
    {
        _store.Has(_logos.GetCachedLogoPath(101)).Returns(true);
        _store.Has(_logos.GetCachedUnitLogoPath(101, 5)).Returns(true);

        Assert.Equal(_logos.GetCachedLogoPath(101), _logos.TryGetCachedLogoPath(101));
        Assert.Equal(_logos.GetCachedUnitLogoPath(101, 5), _logos.TryGetCachedUnitLogoPath(101, 5));
        Assert.True(_logos.IsCached(_logos.GetCachedLogoPath(101)));
    }

    [Fact]
    public void TryGetCachedPaths_NotCached_AreNull()
    {
        Assert.Null(_logos.TryGetCachedLogoPath(101));
        Assert.Null(_logos.TryGetCachedUnitLogoPath(101, 5));
    }

    [Fact]
    public void PackagedPaths_Ids_NameTheBundledFiles()
    {
        Assert.Equal("SVGCache/factions/101.svg", _logos.GetPackagedFactionLogoPath(101));
        Assert.Equal("SVGCache/units/101-5.svg", _logos.GetPackagedUnitLogoPath(101, 5));
    }

    private static MetadataFaction Faction(int id, string? logo) => new() { Id = id, Name = $"Faction {id}", Logo = logo };
}
