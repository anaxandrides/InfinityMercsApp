using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Dtos;

namespace InfinityMercsApp.Infrastructure.Json.Converters;

internal sealed class ArmyFilterSectionConverter : JsonConverter<ArmyFilterSectionDto>
{
    private static readonly LenientObjectListConverter<ArmyFilterItemDto> ListConverter = new();

    public override ArmyFilterSectionDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new() { Items = ListConverter.Read(ref reader, typeof(List<ArmyFilterItemDto>), options) ?? [] };

    public override void Write(Utf8JsonWriter writer, ArmyFilterSectionDto value, JsonSerializerOptions options) =>
        ListConverter.Write(writer, value.Items, options);
}
