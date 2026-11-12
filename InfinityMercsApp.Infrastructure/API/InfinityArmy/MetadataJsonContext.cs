using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Models.API.Metadata;

namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

/// <summary>Source-generated JSON contract for Corvus Belli's metadata, read with <see cref="InfinityArmyAPI"/>'s lenient options.</summary>
[JsonSerializable(typeof(MetadataDocument))]
internal sealed partial class MetadataJsonContext : JsonSerializerContext;
