using System.IO.Compression;
using InfinityMercsApp.Api.ArmyData.Logos;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.ArmyData.Logos;

public sealed class LogoBundlerTests
{
    private readonly ILogoSource _source = Substitute.For<ILogoSource>();
    private readonly ILogoBundleStore _store = Substitute.For<ILogoBundleStore>();

    [Fact]
    public async Task BuildAsync_SomeDownloaded_ZipsThoseUnderTheirPaths()
    {
        _source.GetAsync("https://x/a", Arg.Any<CancellationToken>()).Returns("<svg a/>"u8.ToArray());
        _source.GetAsync("https://x/b", Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        var zips = new List<byte[]>();
        await _store.SaveAsync("h1", Arg.Do<Stream>(zip => zips.Add(((MemoryStream)zip).ToArray())), Arg.Any<CancellationToken>());

        var bundled = await new LogoBundler(_source, _store).BuildAsync("h1", [new LogoFile("units/101-1.svg", "https://x/a"), new LogoFile("101.svg", "https://x/b")]);

        Assert.Equal(1, bundled);
        using var archive = new ZipArchive(new MemoryStream(Assert.Single(zips)));
        var entry = Assert.Single(archive.Entries);
        Assert.Equal("units/101-1.svg", entry.FullName);
        using var reader = new StreamReader(entry.Open());
        Assert.Equal("<svg a/>", await reader.ReadToEndAsync());
    }
}
