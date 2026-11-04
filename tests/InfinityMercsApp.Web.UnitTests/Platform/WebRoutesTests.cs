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

    [Theory]
    [InlineData("season/s1/rank-up", "season/s1")]
    [InlineData("season/my%20season/downtime?x=1", "season/my%20season")]
    public void SeasonOf_StepAddress_IsItsSeason(string address, string expected) => Assert.Equal(expected, WebRoutes.SeasonOf(address));

    [Theory]
    [InlineData("season/s1")]
    [InlineData("companies/new/build")]
    [InlineData("")]
    public void SeasonOf_OtherAddress_IsNull(string address) => Assert.Null(WebRoutes.SeasonOf(address));
}
