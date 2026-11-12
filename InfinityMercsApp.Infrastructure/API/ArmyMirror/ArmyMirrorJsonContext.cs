using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>Source-generated JSON contract for the army data copy's manifest.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ArmyMirrorManifest))]
internal sealed partial class ArmyMirrorJsonContext : JsonSerializerContext;
