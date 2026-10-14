using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json.Dtos;

/// <summary>An army <c>filters</c> section; a non-array value reads as an empty section.</summary>
[JsonConverter(typeof(ArmyFilterSectionConverter))]
internal sealed class ArmyFilterSectionDto
{
    public List<ArmyFilterItemDto> Items { get; init; } = [];
}
