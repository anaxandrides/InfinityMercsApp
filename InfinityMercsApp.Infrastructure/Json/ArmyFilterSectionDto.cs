using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>An army <c>filters</c> section; a non-array value reads as an empty section.</summary>
[JsonConverter(typeof(ArmyFilterSectionConverter))]
internal sealed class ArmyFilterSectionDto
{
    public List<ArmyFilterItemDto> Items { get; init; } = [];
}
