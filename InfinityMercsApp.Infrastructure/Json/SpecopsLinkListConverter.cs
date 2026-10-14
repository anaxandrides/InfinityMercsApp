using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// The items a spec-ops choice grants: bare ids (numbers or numeric strings) or <c>{"id":..,"extras":[..]}</c>
/// objects. Items without a usable id are skipped; a non-array value reads as <c>null</c>.
/// </summary>
internal sealed class SpecopsLinkListConverter : JsonConverter<List<SpecopsLinkDto>?>
{
    private static readonly IdListConverter ExtrasConverter = new();

    public override bool HandleNull => true;

    public override List<SpecopsLinkDto>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var links = new List<SpecopsLinkDto>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (FlexibleIdConverter.ReadId(ref reader) is int bareId)
                {
                    links.Add(new SpecopsLinkDto(bareId, []));
                }

                continue;
            }

            int? id = null;
            List<int>? extras = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var property = reader.GetString();
                reader.Read();
                switch (property)
                {
                    case "id":
                        id = FlexibleIdConverter.ReadId(ref reader);
                        break;
                    case "extras":
                        extras = ExtrasConverter.Read(ref reader, typeof(List<int>), options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (id is int linkId)
            {
                links.Add(new SpecopsLinkDto(linkId, extras ?? []));
            }
        }

        return links;
    }

    public override void Write(Utf8JsonWriter writer, List<SpecopsLinkDto>? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var link in value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", link.Id);
            writer.WritePropertyName("extras");
            ExtrasConverter.Write(writer, link.Extras, options);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
