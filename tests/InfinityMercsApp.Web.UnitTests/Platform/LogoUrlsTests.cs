using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Web.Platform;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>Where the site's images load each logo from.</summary>
public sealed class LogoUrlsTests
{
    private readonly RemoteLogoStore _logos = new();
    private readonly IAppPackageFiles _package = Substitute.For<IAppPackageFiles>();
    private readonly LogoUrls _urls;

    public LogoUrlsTests() => _urls = new LogoUrls(_logos, _package);

    [Fact]
    public async Task ForAsync_EachKind_GivesItsAddressInOrderOnce()
    {
        await _logos.FetchAsync("https://cdn.example/7.svg", "/mercs/svg-cache/units/101-7.svg", CancellationToken.None);
        _package.ExistsAsync("SVGCache\\units\\fusilier.svg").Returns(true);

        var urls = await _urls.ForAsync(
        [
            "/mercs/svg-cache/units/101-7.svg",
            "SVGCache\\units\\fusilier.svg",
            "HTTPS://cdn.example/8.svg",
            "http://cdn.example/9.svg",
            "https://cdn.example/7.svg",
            "  ",
            "SVGCache/units/missing.svg",
            "/mercs/svg-cache/unknown.svg"
        ]);

        Assert.Equal(
            ["https://cdn.example/7.svg", "package/SVGCache/units/fusilier.svg", "HTTPS://cdn.example/8.svg", "http://cdn.example/9.svg"],
            urls);
    }

    [Fact]
    public async Task ForAsync_NoCandidates_IsEmpty() => Assert.Empty(await _urls.ForAsync(null));
}
