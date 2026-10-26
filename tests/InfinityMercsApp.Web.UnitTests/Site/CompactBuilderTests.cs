using Bunit;
using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>On a phone-width window the builder shows one pane at a time, chosen by its tabs.</summary>
public sealed class CompactBuilderTests() : SiteContext(windowWidth: 400)
{
    [Fact]
    public void Tabs_Chosen_ShowThatPane()
    {
        var page = BuildPanOceania();
        page.WaitForAssertion(() => Assert.Equal(3, page.FindAll("[role=tab]").Count), Patience);

        page.FindAll("[role=tab]")[2].Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".roster-pane")), Patience);
        Assert.Empty(page.FindAll(".units-pane"));

        page.FindAll("[role=tab]")[1].Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".profile-pane")), Patience);

        page.FindAll("[role=tab]")[0].Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".units-pane")), Patience);
    }

    [Fact]
    public void FactionStrip_HiddenAndShown_FollowsTheButtons()
    {
        var page = BuildPanOceania();
        Assert.NotNull(Get<CompanySelectionSession>().Current);

        page.WaitForAssertion(() => page.FindAll(".faction-slots button.link").First(button => button.TextContent.Contains("factions", StringComparison.Ordinal)).Click(), Patience);
        page.WaitForAssertion(() => page.FindAll(".faction-slots button.link").First().Click(), Patience);

        Assert.NotEmpty(page.FindAll(".faction-slot"));
    }
}
