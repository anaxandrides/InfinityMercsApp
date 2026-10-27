using Bunit;
using InfinityMercsApp.Web.Pages;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>The home page: an invitation to sign in, or the account's companies and seasons.</summary>
public sealed class HomeTests : SiteContext
{
    private readonly AccountFiles _account;

    public HomeTests() => _account = new AccountFiles(Api);

    [Fact]
    public void Render_SignedOut_InvitesSigningIn()
    {
        Authorization.SetNotAuthorized();

        Assert.Contains("authentication/login", Render<Home>().Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_SignedIn_ListsCompaniesAndSeasons()
    {
        Authorization.SetAuthorized("Kestrel");
        _account.Companies(TestFiles.Company("Blue Lions"));
        _account.Seasons(TestFiles.Season("Blue Lions"));

        var home = Render<Home>();

        home.WaitForAssertion(() => Assert.Equal(2, home.FindAll(".card").Count));
        Assert.Equal("companies/c1", home.FindAll(".card")[0].GetAttribute("href"));
        Assert.Contains("2 troopers", home.FindAll(".card")[0].TextContent, StringComparison.Ordinal);
        Assert.Equal("seasons/s1", home.FindAll(".card")[1].GetAttribute("href"));
        Assert.Contains("1 won", home.FindAll(".card")[1].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_NothingInTheAccount_SaysSo()
    {
        Authorization.SetAuthorized("Kestrel");
        _account.Companies();
        _account.Seasons();

        var home = Render<Home>();

        home.WaitForAssertion(() => Assert.Contains("No companies yet", home.Markup, StringComparison.Ordinal));
        Assert.Contains("No seasons yet", home.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ServerUnreachable_SaysSoForBoth()
    {
        Authorization.SetAuthorized("Kestrel");
        _account.Unreachable();

        var home = Render<Home>();

        home.WaitForAssertion(() => Assert.Equal(2, home.FindAll("[role=alert]").Count));
    }
}
