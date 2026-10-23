using InfinityMercsApp.Api.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace InfinityMercsApp.Api.UnitTests.Telemetry;

public sealed class ApplicationInsightsTelemetrySinkTests
{
    private readonly CapturingChannel _channel = new();
    private readonly ApplicationInsightsTelemetrySink _sink;

    public ApplicationInsightsTelemetrySinkTests()
    {
        var configuration = new TelemetryConfiguration { TelemetryChannel = _channel, ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000" };
        _sink = new ApplicationInsightsTelemetrySink(new TelemetryClient(configuration));
    }

    [Fact]
    public void Write_PageShown_IsAnEventWithTheJavaObjectCount()
    {
        _sink.Write(TelemetryBatches.With(TelemetryBatches.PageShown), userId: null);

        var record = Assert.IsType<EventTelemetry>(Assert.Single(_channel.Sent));
        Assert.Equal("PageShown", record.Name);
        Assert.Equal("MercsSeasonPage", record.Properties["Name"]);
        Assert.Equal(12345, record.Metrics["JavaObjects"]);
        Assert.Equal(TelemetryBatches.Now, record.Timestamp);
    }

    [Fact]
    public void Write_Crash_IsAnExceptionWithItsTypeMessageAndStack()
    {
        _sink.Write(TelemetryBatches.With(TelemetryBatches.Crash), userId: null);

        var record = Assert.IsType<ExceptionTelemetry>(Assert.Single(_channel.Sent));
        var details = Assert.Single(record.ExceptionDetailsInfoList);
        Assert.Equal("System.InvalidOperationException", details.TypeName);
        Assert.Equal("Sequence contains no elements", details.Message);
        Assert.Equal(SeverityLevel.Critical, record.SeverityLevel);
    }

    [Fact]
    public void Write_AnyEvent_CarriesTheInstallAndDevice()
    {
        _sink.Write(TelemetryBatches.With(TelemetryBatches.PageShown), userId: null);

        var record = Assert.Single(_channel.Sent);
        Assert.Equal(ApplicationInsightsTelemetrySink.RoleName, record.Context.Cloud.RoleName);
        Assert.Equal("6f9619ff-8b86-d011-b42d-00c04fc964ff", record.Context.User.Id);
        Assert.Equal("0.1.0", record.Context.Component.Version);
        Assert.Equal("Android 16", record.Context.Device.OperatingSystem);
        Assert.Equal("Google Pixel 10", record.Context.Device.Model);
        Assert.Null(record.Context.User.AuthenticatedUserId);
    }

    [Fact]
    public void Write_SignedIn_CarriesTheUserId()
    {
        _sink.Write(TelemetryBatches.With(TelemetryBatches.PageShown), "user-1");

        Assert.Equal("user-1", Assert.Single(_channel.Sent).Context.User.AuthenticatedUserId);
    }
}
