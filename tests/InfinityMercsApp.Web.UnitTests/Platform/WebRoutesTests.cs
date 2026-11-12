using InfinityMercsApp.Services.Navigation;
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

    [Fact]
    public void ForLeague_LeaguePageWithoutItsLeague_IsNull() => Assert.Null(WebRoutes.ForLeague("LeaguePage", null));

    [Fact]
    public void ForLeague_OtherPage_IsNull() => Assert.Null(WebRoutes.ForLeague("SeasonPage", NavigationParameters.ForLeague("l1")));

    [Fact]
    public void LeagueCompany_IdsNeedingEscapes_AreEscaped() => Assert.Equal("leagues/a%20b/companies/c%2Fd", WebRoutes.LeagueCompany("a b", "c/d"));

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
