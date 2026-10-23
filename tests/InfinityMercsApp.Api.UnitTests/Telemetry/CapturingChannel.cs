using Microsoft.ApplicationInsights.Channel;

namespace InfinityMercsApp.Api.UnitTests.Telemetry;

/// <summary>Keeps what an Application Insights client sends, instead of sending it.</summary>
internal sealed class CapturingChannel : ITelemetryChannel
{
    public List<ITelemetry> Sent { get; } = [];

    public bool? DeveloperMode { get; set; }

    public string EndpointAddress { get; set; } = string.Empty;

    public void Send(ITelemetry item) => Sent.Add(item);

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}
