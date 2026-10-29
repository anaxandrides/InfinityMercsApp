using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.ApiClient.Telemetry;

namespace InfinityMercsApp.ApiClient;

/// <summary>Source-generated JSON contracts for what the client sends of its own, so no reflection is needed.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(TelemetryBatch))]
internal sealed partial class ApiClientJsonContext : JsonSerializerContext;
