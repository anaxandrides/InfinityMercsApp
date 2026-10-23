using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Services.Telemetry;

namespace InfinityMercsApp.UnitTests.Services.Telemetry;

public sealed class TelemetryEventsTests
{
    [Fact]
    public void Crash_InnerException_NamesTheOuterAndListsTheInner()
    {
        var exception = new InvalidOperationException("Outer", new ArgumentException("Inner"));

        var crash = TelemetryEvents.Crash("AppDomain.UnhandledException", exception, TempTelemetry.Now);

        Assert.Equal(TelemetryKind.Crash, crash.Kind);
        Assert.Equal("System.InvalidOperationException", crash.Name);
        Assert.Equal("AppDomain.UnhandledException: Outer", crash.Detail);
        Assert.Contains("--- System.ArgumentException: Inner", crash.StackTrace, StringComparison.Ordinal);
    }

    [Fact]
    public void Crash_LongMessage_IsCutToTheLimit()
    {
        var crash = TelemetryEvents.Crash("Test", new InvalidOperationException(new string('m', 10_000)), TempTelemetry.Now);

        Assert.Equal(TelemetryLimits.MaxDetailLength, crash.Detail?.Length);
    }

    [Fact]
    public void Startup_NegativeTime_IsZero() =>
        Assert.Equal(0, TelemetryEvents.Startup("FirstPage", -5, TempTelemetry.Now).Value);

    [Fact]
    public void PageShown_NoJavaObjectCount_HasNoValue() =>
        Assert.Null(TelemetryEvents.PageShown("SettingsPage", javaObjects: null, TempTelemetry.Now).Value);
}
