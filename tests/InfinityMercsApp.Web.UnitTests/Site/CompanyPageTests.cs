using Bunit;
using Bunit.TestDoubles;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Web.Pages;
using InfinityMercsApp.Web.Platform;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>A company's page: its troopers, and starting a season with it.</summary>
public sealed class CompanyPageTests : SiteContext
{
    private readonly AccountFiles _account;

    public CompanyPageTests()
    {
        _account = new AccountFiles(Api);
        Authorization.SetAuthorized("Kestrel");
    }

    private IRenderedComponent<CompanyPage> Page(string id = "c1") => Render<CompanyPage>(parameters => parameters.Add(p => p.Id, id));

    [Fact]
    public void Render_Company_ListsItsTroopers()
    {
        _account.Companies(TestFiles.Company("Blue Lions"));

        var page = Page();

        page.WaitForAssertion(() => Assert.Equal("Blue Lions", page.Find("h1").TextContent));
        Assert.Contains("Captain", page.FindAll(".trooper")[0].TextContent, StringComparison.Ordinal);
        Assert.Contains(page.FindAll(".trooper"), trooper => trooper.TextContent.Contains("Rook", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_Unreachable_SaysSo()
    {
        _account.Unreachable();

        var page = Page();

        page.WaitForAssertion(() => Assert.Contains("Couldn't reach the server", page.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void StartSeason_CompanyNotSyncedYet_SaysSo()
    {
        _account.Companies(TestFiles.Company("Blue Lions"));
        var page = Page();

        page.WaitForElement("button.primary").Click();

        page.WaitForAssertion(() => Assert.Contains("hasn't synced", page.Find(".danger").TextContent, StringComparison.Ordinal));
        Assert.False(page.Find("button.primary").HasAttribute("disabled"));
    }

    [Fact]
    public async Task StartSeason_CompanyHere_OpensTheSeasonWithIt()
    {
        _account.Companies(TestFiles.Company("Blue Lions"));
        var path = Path.Combine(Paths.CompaniesDirectory, "blue-lions-0001.json");
        Directory.CreateDirectory(Paths.CompaniesDirectory);
        await Get<ICompanyFileService>().SaveAsync(path, TestFiles.Company("Blue Lions"));
        var page = Page();

        page.WaitForElement("button.primary").Click();

        var navigation = (BunitNavigationManager)Get<NavigationManager>();
        page.WaitForAssertion(() => Assert.EndsWith("/season", navigation.Uri, StringComparison.Ordinal));
        Assert.Equal(path, Get<NavigationQuery>().Take()?.Values.OfType<string>().First());
    }
}
