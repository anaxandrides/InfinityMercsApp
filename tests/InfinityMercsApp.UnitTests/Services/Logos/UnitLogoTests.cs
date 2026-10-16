using InfinityMercsApp.Services.Logos;

namespace InfinityMercsApp.UnitTests.Services.Logos;

public sealed class UnitLogoTests
{
    [Fact]
    public void CandidatesRunFromDownloadedToPackagedToFallback() =>
        Assert.Equal(
            ["/data/svg-cache/units/101-1.svg", "SVGCache/units/101-1.svg", "SVGCache/CBIcons/tag.svg"],
            UnitLogo.Candidates("/data/svg-cache/units/101-1.svg", "SVGCache/units/101-1.svg", "SVGCache/CBIcons/tag.svg"));

    [Fact]
    public void PackagedPathsBecomeRelativeAssetPaths() =>
        Assert.Equal(["SVGCache/units/101-1.svg"], UnitLogo.Candidates(null, @"\SVGCache\units\101-1.svg"));

    [Fact]
    public void BlankAndRepeatedPathsAreLeftOut() =>
        Assert.Equal(["SVGCache/a.svg"], UnitLogo.Candidates(" ", "SVGCache/a.svg", "SVGCache/a.svg"));
}
