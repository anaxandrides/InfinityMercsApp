using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>Source-generated JSON contracts for an army sent whole (<see cref="ArmyDefinitionJson"/>).</summary>
[JsonSerializable(typeof(ArmyDefinition))]
[JsonSerializable(typeof(StoredArmyFilterCatalog))]
internal sealed partial class ArmyDefinitionJsonContext : JsonSerializerContext;
