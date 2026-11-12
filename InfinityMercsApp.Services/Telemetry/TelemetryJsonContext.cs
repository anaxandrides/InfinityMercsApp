using System.Text.Json.Serialization;
using InfinityMercsApp.ApiClient.Telemetry;

namespace InfinityMercsApp.Services.Telemetry;

/// <summary>Source-generated JSON contracts for the telemetry outbox and setting.</summary>
[JsonSerializable(typeof(TelemetryEvent))]
[JsonSerializable(typeof(TelemetrySettings))]
internal sealed partial class TelemetryJsonContext : JsonSerializerContext;
