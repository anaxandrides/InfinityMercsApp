using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>The app's bundled files, which the site serves under package/ and lists in package/files.txt.</summary>
public sealed class WebPackageFilesTests
{
    private readonly PackageServer _server = new();
    private readonly WebPackageFiles _files;

    public WebPackageFilesTests() => _files = new WebPackageFiles(new HttpClient(_server), new TestNavigation());

    [Fact]
    public async Task ExistsAsync_Listed_IsTrueWhateverItsSlashesOrCase()
    {
        Assert.True(await _files.ExistsAsync("\\SVGCache\\Units\\fusilier.svg"));
        Assert.False(await _files.ExistsAsync("SVGCache/units/missing.svg"));
        Assert.Equal(1, _server.ListRequests);
    }

    [Fact]
    public async Task ReadTextAsync_Packaged_ReadsItFromPackage()
    {
        Assert.Equal("glossary", await _files.ReadTextAsync("Glossary/glossary.json"));
        Assert.Contains("https://site.example/package/Glossary/glossary.json", _server.Requested);
    }

    [Fact]
    public async Task OpenAsync_Packaged_StreamsIt()
    {
        await using var stream = await _files.OpenAsync("SVGCache/units/fusilier.svg");

        Assert.Equal("<svg/>", await new StreamReader(stream).ReadToEndAsync());
    }

    [Fact]
    public async Task OpenAsync_NotPackaged_IsFileNotFound() =>
        await Assert.ThrowsAsync<FileNotFoundException>(() => _files.OpenAsync("SVGCache/units/missing.svg"));

    [Fact]
    public async Task OpenAsync_ServerFails_Throws() =>
        await Assert.ThrowsAsync<HttpRequestException>(() => _files.OpenAsync("broken.json"));
}
