using System.Net;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.UnitTests.InfinityArmy;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Logos;

public sealed class LogoFileStoreTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("logo-store-").FullName;
    private readonly IAppPackageFiles _package = Substitute.For<IAppPackageFiles>();

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string LogoPath => Path.Combine(_folder, "units", "101-1.svg");

    [Fact]
    public async Task FetchAsync_Served_WritesTheLogo()
    {
        var store = new LogoFileStore(new HttpClient(new RecordingHandler(HttpStatusCode.OK, new StringContent("<svg/>"))), _package);

        Assert.True(await store.FetchAsync("https://cdn.example/1.svg", LogoPath, CancellationToken.None));

        Assert.True(store.Has(LogoPath));
        Assert.False(File.Exists(LogoPath + ".part"));
    }

    [Fact]
    public async Task FetchAsync_NotServed_IsFalse()
    {
        var store = new LogoFileStore(new HttpClient(new RecordingHandler(HttpStatusCode.NotFound, new StringContent(string.Empty))), _package);

        Assert.False(await store.FetchAsync("https://cdn.example/1.svg", LogoPath, CancellationToken.None));
        Assert.False(store.Has(LogoPath));
    }

    [Fact]
    public async Task FetchAsync_Unreachable_IsFalse() =>
        Assert.False(await new LogoFileStore(new HttpClient(new ThrowingHandler()), _package).FetchAsync("https://cdn.example/1.svg", LogoPath, CancellationToken.None));

    [Fact]
    public async Task CopyPackagedAsync_Packaged_CopiesIt()
    {
        _package.OpenAsync("SVGCache/units/101-1.svg").Returns(_ => new MemoryStream("<svg/>"u8.ToArray()));
        var store = new LogoFileStore(new HttpClient(new ThrowingHandler()), _package);

        Assert.True(await store.CopyPackagedAsync("SVGCache/units/101-1.svg", LogoPath, CancellationToken.None));

        Assert.True(store.Has(LogoPath));
    }

    [Fact]
    public async Task CopyPackagedAsync_NotPackaged_IsFalse()
    {
        _package.OpenAsync(Arg.Any<string>()).Returns<Stream>(_ => throw new FileNotFoundException("Not packaged."));

        Assert.False(await new LogoFileStore(new HttpClient(new ThrowingHandler()), _package).CopyPackagedAsync("SVGCache/x.svg", LogoPath, CancellationToken.None));
    }

    [Fact]
    public void Has_EmptyFile_IsFalse()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "units"));
        File.WriteAllText(LogoPath, string.Empty);

        Assert.False(new LogoFileStore(new HttpClient(new ThrowingHandler()), _package).Has(LogoPath));
    }
}
