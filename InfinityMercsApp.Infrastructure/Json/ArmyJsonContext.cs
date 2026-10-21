using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Dtos;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// Source-generated System.Text.Json metadata for the Infinity Army payloads. Reading through this context
/// needs no reflection, so it is trim- and AOT-safe on iOS and Android.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<ProfileGroupDto>), TypeInfoPropertyName = "ProfileGroups")]
[JsonSerializable(typeof(UnitFiltersDto), TypeInfoPropertyName = "UnitFilters")]
[JsonSerializable(typeof(Dictionary<string, ArmyFilterSectionDto>), TypeInfoPropertyName = "ArmyFilters")]
[JsonSerializable(typeof(FireteamChartDto), TypeInfoPropertyName = "FireteamChart")]
[JsonSerializable(typeof(FireteamTeamDto))]
[JsonSerializable(typeof(FireteamUnitDto))]
[JsonSerializable(typeof(ArmyFilterItemDto))]
[JsonSerializable(typeof(ProfileDto))]
[JsonSerializable(typeof(ProfileOptionDto))]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(IncludeDto))]
[JsonSerializable(typeof(List<ProfileOptionDto>), TypeInfoPropertyName = "ProfileOptions")]
[JsonSerializable(typeof(List<ArmyRelationDto>), TypeInfoPropertyName = "Relations")]
[JsonSerializable(typeof(SpecopsDto), TypeInfoPropertyName = "Specops")]
[JsonSerializable(typeof(ArmyPayloadDto), TypeInfoPropertyName = "ArmyPayload")]
[JsonSerializable(typeof(ArmyUnitDto))]
[JsonSerializable(typeof(ArmyResumeDto))]
[JsonSerializable(typeof(ArmyRelationUnitDto))]
[JsonSerializable(typeof(SpecopsChoiceDto))]
[JsonSerializable(typeof(JsonScalar))]
internal sealed partial class ArmyJsonContext : JsonSerializerContext
{
}
