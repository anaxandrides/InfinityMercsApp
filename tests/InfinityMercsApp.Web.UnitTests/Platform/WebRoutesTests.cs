using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

public sealed class WebRoutesTests
{
    [Theory]
    [InlineData("//CreateNewCompanyPage", WebRoutes.NewCompany)]
    [InlineData("//StandardCompanySourcePopupPage", WebRoutes.CompanySource)]
    [InlineData("//MercsSeasonPage", WebRoutes.Home)]
    public void For_KnownRoute_IsTheSitesAddress(string route, string expected) => Assert.Equal(expected, WebRoutes.For(route));

    [Fact]
    public void For_PageTheSiteLacks_IsNull() => Assert.Null(WebRoutes.For("ArmoryPage"));
}
