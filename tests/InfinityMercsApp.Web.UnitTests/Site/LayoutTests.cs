using Bunit;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.Web.Layout;
using InfinityMercsApp.Web.Pages;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>The site's frame: the router, the header's sign-in, the footer's diagnostics switch, and page telemetry.</summary>
public sealed class LayoutTests : SiteContext
{
    private NavigationManager Navigation => Get<NavigationManager>();

    private IReadOnlyList<TelemetryEvent> Recorded()
    {
        var store = Get<TelemetryStore>();
        return store.TakePending().SelectMany(TelemetryStore.Read).ToList();
    }

    [Fact]
    public void App_Home_ShowsItInTheLayoutAndReportsIt()
    {
        Authorization.SetNotAuthorized();
        Get<TelemetryStore>().SetEnabled(true);

        var app = Render<App>();

        app.WaitForAssertion(() => Assert.Contains("INFINITY MERCS", app.Find("header").TextContent, StringComparison.Ordinal), Patience);
        Assert.NotEmpty(app.FindAll("footer .diagnostics"));
        Assert.Contains(Recorded(), e => e.Kind == TelemetryKind.PageShown && e.Name == nameof(Home));
    }

    [Fact]
    public void RedirectToLogin_Shown_GoesToSignIn()
    {
        Render<RedirectToLogin>();

        Assert.EndsWith("authentication/login", Navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void App_AccountPageWhileSigningIn_SaysSo()
    {
        Authorization.SetAuthorizing();
        Navigation.NavigateTo("companies/c1");

        var app = Render<App>();

        app.WaitForAssertion(() => Assert.Contains("Signing in", app.Markup, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public void NotFound_Always_OffersTheWayBack() =>
        Assert.Equal("Not found", Render<NotFound>().Find("h1").TextContent);

    [Fact]
    public void LoginDisplay_SignedIn_NamesTheUserAndSignsOut()
    {
        Authorization.SetAuthorized("Kestrel");

        var display = Render<LoginDisplay>();
        Assert.Contains("Kestrel", display.Markup, StringComparison.Ordinal);
        display.Find("button").Click();

        Assert.EndsWith("authentication/logout", Navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginDisplay_SignedOut_OffersSignIn()
    {
        Authorization.SetNotAuthorized();

        Assert.Equal("authentication/login", Render<LoginDisplay>().Find("a").GetAttribute("href"));
    }

    [Fact]
    public void DiagnosticsToggle_TurnedOff_KeepsTheSettingInTheBrowser()
    {
        var settings = Get<ITelemetrySettings>();
        settings.SetEnabled(true);
        var toggle = Render<DiagnosticsToggle>();
        Assert.True(toggle.Find("input").HasAttribute("checked"));

        toggle.Find("input").Change(false);

        Assert.False(settings.IsEnabled);
        // Saving reads the setting's file before handing it to the browser, after the change event has returned.
        toggle.WaitForAssertion(() => Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "saveSettings"), Patience);
    }
}
