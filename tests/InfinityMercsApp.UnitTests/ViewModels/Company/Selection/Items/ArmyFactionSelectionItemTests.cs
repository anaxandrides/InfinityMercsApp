using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection.Items;

public sealed class ArmyFactionSelectionItemTests
{
    [Fact]
    public void LogosFallBackToThePackagedLogoById() =>
        Assert.Equal(
            ["/data/svg-cache/101.svg", "SVGCache/factions/101.svg"],
            new ArmyFactionSelectionItem { Id = 101, CachedLogoPath = "/data/svg-cache/101.svg", PackagedLogoPath = "SVGCache/factions/101.svg" }.LogoCandidates);

    [Fact]
    public void AGeneratedCompanyTriesItsOwnLogoFirst() =>
        Assert.Equal(
            ["SVGCache/CBIcons/tag.svg", "SVGCache/factions/2003.svg"],
            new ArmyFactionSelectionItem { Id = 2003, PackagedLogoPath = "SVGCache/CBIcons/tag.svg" }.LogoCandidates);
}
