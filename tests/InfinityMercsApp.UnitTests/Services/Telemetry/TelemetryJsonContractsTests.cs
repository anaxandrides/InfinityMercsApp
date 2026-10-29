using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Telemetry;

/// <summary>The telemetry outbox and setting read and write as they did through reflection.</summary>
public sealed class TelemetryJsonContractsTests
{
    private static readonly JsonSerializerOptions Generated = new(JsonSerializerDefaults.Web) { TypeInfoResolver = TelemetryJsonContext.Default };

    private static readonly JsonSerializerOptions ByReflection = new(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    [Fact]
    public void Event_EveryMemberFilled_WritesAsReflectionDid()
    {
        var telemetryEvent = FilledObjects.Of<TelemetryEvent>();

        Assert.Equal(JsonSerializer.Serialize(telemetryEvent, ByReflection), JsonSerializer.Serialize(telemetryEvent, JsonContracts.For<TelemetryEvent>(Generated)));
    }

    [Fact]
    public void Settings_Saved_ReadBack()
    {
        var settings = new TelemetrySettings("6f9619ff-8b86-d011-b42d-00c04fc964ff", IsEnabled: false);
        var json = JsonSerializer.Serialize(settings, ByReflection);

        Assert.Equal(json, JsonSerializer.Serialize(settings, JsonContracts.For<TelemetrySettings>(Generated)));
        Assert.Equal(settings, JsonSerializer.Deserialize(json, JsonContracts.For<TelemetrySettings>(Generated)));
    }
}
