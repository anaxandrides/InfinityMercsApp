using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Units;

public sealed class UnitFilterOptionsCatalogTests
{
    [Theory]
    [InlineData("75", 200)]
    [InlineData("300", 300)]
    [InlineData("lots", 200)]
    public void TheSliderRunsToThePointsLimitButAtLeast200(string limit, int max) =>
        Assert.Equal(max, UnitFilterOptionsCatalog.MaxPointsFor(limit));

    [Fact]
    public void BeforeAnyOptionsAreReadTheFilterOffersOnlyPoints()
    {
        var options = UnitFilterOptionsCatalog.WithMaxPoints(null, 150);

        Assert.Empty(options.Skills);
        Assert.Equal(150, options.MaxPoints);
    }

    [Fact]
    public void ACopyKeepsTheChoicesWithTheNewSlider()
    {
        var options = UnitFilterOptionsCatalog.WithMaxPoints(new UnitFilterPopupOptions { Skills = ["Camouflage"], MaxPoints = 200 }, 120);

        Assert.Equal(["Camouflage"], options.Skills);
        Assert.Equal(120, options.MaxPoints);
    }
}
