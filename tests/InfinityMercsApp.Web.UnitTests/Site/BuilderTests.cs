using Bunit;
using Bunit.TestDoubles;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Web.Pages;
using InfinityMercsApp.Web.Platform;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>The company builder, driven as a player does: the kind of company, its faction, a unit and its profiles.</summary>
public sealed class BuilderTests : SiteContext
{
    private BunitNavigationManager Navigation => (BunitNavigationManager)Get<NavigationManager>();

    [Fact]
    public void NewCompany_StandardCompany_AsksWhereItsTroopersComeFrom()
    {
        var page = Render<NewCompany>();

        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".company-choice")), Patience);
        Assert.Contains("log in", page.Markup, StringComparison.Ordinal);
        // The logos resolve after the choices show, redrawing them; the click is tried until it lands.
        page.WaitForAssertion(() => page.FindAll(".company-choice").First(choice => choice.TextContent.Contains("Standard", StringComparison.Ordinal)).Click(), Patience);

        page.WaitForAssertion(() => Assert.EndsWith("/companies/new/source", Navigation.Uri, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public void CompanySelection_NothingBeingBuilt_SaysSo()
    {
        var page = Render<CompanySelection>();

        page.WaitForAssertion(() => Assert.Contains("Nothing being built", page.Markup, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public void Build_VanillaPanOceania_HiresAUnitsProfile()
    {
        Authorization.SetAuthorized("Kestrel");
        var source = Render<CompanySource>();
        source.WaitForAssertion(() => Assert.Equal(2, source.FindAll(".company-choice").Count), Patience);
        source.FindAll(".company-choice")[0].Click();
        source.WaitForAssertion(() => Assert.NotNull(Get<CompanySelectionSession>().Current), Patience);

        var page = Render<CompanySelection>();
        page.WaitForAssertion(() => Assert.Contains(page.FindAll(".faction-tile"), tile => tile.TextContent.Contains("PanOceania", StringComparison.Ordinal)), Patience);
        page.FindAll(".faction-tile").First(tile => tile.TextContent.Contains("PanOceania", StringComparison.Ordinal)).Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".unit-row")), Patience);

        page.FindAll(".unit-row")[0].Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".profile-card")), Patience);
        Assert.NotEmpty(page.FindAll(".unit-header"));
        page.FindAll(".profile-card").First(card => !card.ClassList.Contains("blocked")).Click();

        page.WaitForAssertion(() => Assert.Single(page.FindAll(".entry-card")), Patience);
        page.Find(".entry-actions button").Click();
        Assert.NotEmpty(page.FindAll(".kit"));
        page.Find("#company-name").Input("Blue Lions");
        Assert.Equal("Blue Lions", Get<CompanySelectionSession>().Current?.Roster.CompanyName);
    }

    [Fact]
    public async Task Build_FilteredToFireteams_ListsThemAndTheirTroopers()
    {
        var page = BuildPanOceania();
        var screen = Get<CompanySelectionSession>().Current ?? throw new InvalidOperationException("Nothing is being built.");

        await page.InvokeAsync(() => screen.ApplyUnitFilterCommand.Execute(UnitFilterCriteria.None with { TeamsView = true }));

        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".team-card")), Patience);
        Assert.NotEmpty(page.FindAll(".team-slots .count"));
        page.WaitForAssertion(() => page.FindAll(".team-slots .unit-row")[0].Click(), Patience);
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".unit-header")), Patience);
    }
}
