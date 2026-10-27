using Bunit;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Web.Pages;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>A season's page: its contracts, results and retired troopers.</summary>
public sealed class SeasonPageTests : SiteContext
{
    private readonly AccountFiles _account;

    public SeasonPageTests()
    {
        _account = new AccountFiles(Api);
        Authorization.SetAuthorized("Kestrel");
    }

    private IRenderedComponent<SeasonPage> Page() => Render<SeasonPage>(parameters => parameters.Add(p => p.Id, "s1"));

    [Fact]
    public void Render_Season_ListsItsContracts()
    {
        _account.Seasons(TestFiles.Season());

        var page = Page();

        page.WaitForAssertion(() => Assert.Equal("Blue Lions", page.Find("h1").TextContent));
        Assert.Equal(2, page.FindAll("tbody tr").Count);
        Assert.Equal("season/s1", page.Find("a.button").GetAttribute("href"));
        Assert.Contains("Rook", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_NoContracts_SaysSo()
    {
        _account.Seasons(new SeasonFile { SeasonIdentifier = "s1", CompanyName = "Blue Lions", CompanyIdentifier = "c1" });

        var page = Page();

        page.WaitForAssertion(() => Assert.Contains("No contracts played yet", page.Markup, StringComparison.Ordinal));
        Assert.Empty(page.FindAll("h2"));
    }

    [Fact]
    public void Render_Unreachable_SaysSo()
    {
        _account.Unreachable();

        var page = Page();

        page.WaitForAssertion(() => Assert.Single(page.FindAll("[role=alert]")));
    }
}
