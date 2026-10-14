using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// Reads an id the way the API sends them: a number, a numeric string, or an object with an <c>id</c>
/// property (recursively). Anything else reads as <c>null</c>.
/// </summary>
internal sealed class FlexibleIdConverter : JsonConverter<int?>
{
    public override bool HandleNull => true;

    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ReadId(ref reader);

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }

    /// <summary>Reads the current value as an id and leaves the reader on its last token.</summary>
    public static int? ReadId(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetInt32(out var number) ? number : null;
            case JsonTokenType.String:
                return int.TryParse(reader.GetString(), out var parsed) ? parsed : null;
            case JsonTokenType.StartObject:
                int? id = null;
                var hasId = false;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var isIdProperty = reader.ValueTextEquals("id"u8);
                    reader.Read();
                    if (isIdProperty)
                    {
                        // Duplicate properties: the last one wins, as with JsonElement.TryGetProperty.
                        hasId = true;
                        id = ReadId(ref reader);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                return hasId ? id : null;
            default:
                reader.Skip();
                return null;
        }
    }
}
