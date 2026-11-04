using Bunit;
using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>Filtering the units on the builder, as the app's filter popup does.</summary>
public sealed class UnitFilterTests : SiteContext
{
    [Fact]
    public void Filter_Opened_ShowsTheCriteria()
    {
        var page = BuildPanOceania();

        page.Find(".pane-tools button").Click();

        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".dialog.unit-filter .chip-button")), Patience);
    }

    [Fact]
    public void Filter_Back_ClosesWithoutFiltering()
    {
        var page = BuildPanOceania();
        page.Find(".pane-tools button").Click();
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".dialog.unit-filter")), Patience);

        page.FindAll(".dialog.unit-filter .buttons button").First(button => button.TextContent == "Back").Click();

        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".dialog.unit-filter")), Patience);
        Assert.Equal("FILTER", page.Find(".pane-tools button").TextContent.Trim());
    }

    [Fact]
    public void Filter_ValueChosenAndUpdated_FiltersTheUnits()
    {
        var page = BuildPanOceania();
        page.Find(".pane-tools button").Click();
        page.WaitForAssertion(() => page.FindAll(".dialog.unit-filter .chip-button")[0].Click(), Patience);
        page.WaitForAssertion(() => Assert.True(page.FindAll(".dialog.unit-filter .chips")[1].Children.Length > 0), Patience);

        page.FindAll(".dialog.unit-filter .chips")[1].Children[0].Click();
        page.FindAll(".dialog.unit-filter .buttons button").First(button => button.TextContent == "Update").Click();

        page.WaitForAssertion(() => Assert.Contains("ON", page.Find(".pane-tools button").TextContent, StringComparison.Ordinal), Patience);
        Assert.True(Get<CompanySelectionSession>().Current?.UnitList.IsFilterActive);
    }

    [Fact]
    public void Filter_Reset_ClearsTheChosenValues()
    {
        var page = BuildPanOceania();
        page.Find(".pane-tools button").Click();
        page.WaitForAssertion(() => page.FindAll(".dialog.unit-filter .chip-button")[0].Click(), Patience);
        page.WaitForAssertion(() => page.FindAll(".dialog.unit-filter .chips")[1].Children[0].Click(), Patience);

        page.FindAll(".dialog.unit-filter .buttons button").First(button => button.TextContent == "Reset").Click();

        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".dialog.unit-filter .chip-button.has-selections")), Patience);
    }
}
