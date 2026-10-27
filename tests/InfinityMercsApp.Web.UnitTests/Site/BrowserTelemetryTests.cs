using Bunit;
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.Web.Platform;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>Telemetry in the browser: its setting kept in local storage, and what waits sent when the tab is hidden.</summary>
public sealed class BrowserTelemetryTests : SiteContext
{
    [Fact]
    public async Task StartAsync_SettingKept_RestoresItAndStartsSending()
    {
        const string kept = """{"installId":"6f9619ff-8b86-d011-b42d-00c04fc964ff","isEnabled":true}""";
        JSInterop.SetupModule("./js/telemetry.js").Setup<string?>("loadSettings").SetResult(kept);

        await Get<BrowserTelemetry>().StartAsync();

        Assert.Equal("6f9619ff-8b86-d011-b42d-00c04fc964ff", Get<TelemetryStore>().InstallId);
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "watchHidden");
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "saveSettings");
        Telemetry.Received(1).Start();
    }

    [Fact]
    public async Task StartAsync_NothingKept_KeepsANewInstallsSetting()
    {
        await Get<BrowserTelemetry>().StartAsync();

        Assert.False(string.IsNullOrEmpty(Get<TelemetryStore>().InstallId));
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "saveSettings");
    }

    [Fact]
    public async Task SaveAsync_NoSettingYet_KeepsNothing()
    {
        await Get<BrowserTelemetry>().SaveAsync();

        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "saveSettings");
    }

    [Fact]
    public void PageHidden_Always_SendsSoon()
    {
        Get<BrowserTelemetry>().PageHidden();

        Telemetry.Received(1).SendSoon();
    }
}
