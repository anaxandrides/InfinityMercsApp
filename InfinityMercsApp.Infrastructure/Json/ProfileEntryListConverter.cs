using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// Reads an entry list item by item, keeping entries that have no usable id (they still count towards
/// quantities and "has any peripheral" checks). A non-array value reads as <c>null</c>.
/// </summary>
internal sealed class ProfileEntryListConverter : JsonConverter<List<ProfileEntryDto>?>
{
    private static readonly JsonScalarConverter ScalarConverter = new();

    public override bool HandleNull => true;

    public override List<ProfileEntryDto>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var entries = new List<ProfileEntryDto>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            entries.Add(ReadEntry(ref reader, options));
        }

        return entries;
    }

    public override void Write(Utf8JsonWriter writer, List<ProfileEntryDto>? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var entry in value)
        {
            if (!entry.IsObject)
            {
                if (entry.Id.HasValue)
                {
                    writer.WriteNumberValue(entry.Id.Value);
                }
                else
                {
                    writer.WriteNullValue();
                }

                continue;
            }

            writer.WriteStartObject();
            if (entry.Id.HasValue)
            {
                writer.WriteNumber("id", entry.Id.Value);
            }

            if (!entry.Q.IsMissing)
            {
                writer.WritePropertyName("q");
                ScalarConverter.Write(writer, entry.Q, options);
            }

            if (!entry.Order.IsMissing)
            {
                writer.WritePropertyName("order");
                ScalarConverter.Write(writer, entry.Order, options);
            }

            if (entry.Extra is not null)
            {
                writer.WriteStartArray("extra");
                foreach (var extra in entry.Extra)
                {
                    if (extra.HasValue)
                    {
                        writer.WriteNumberValue(extra.Value);
                    }
                    else
                    {
                        writer.WriteNullValue();
                    }
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static ProfileEntryDto ReadEntry(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return new ProfileEntryDto { Id = FlexibleIdConverter.ReadId(ref reader) };
        }

        int? id = null;
        JsonScalar q = default;
        JsonScalar order = default;
        List<int?>? extra = null;

        // Duplicate properties: the last one wins, as with JsonElement.TryGetProperty.
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.ValueTextEquals("id"u8))
            {
                reader.Read();
                id = FlexibleIdConverter.ReadId(ref reader);
            }
            else if (reader.ValueTextEquals("q"u8))
            {
                reader.Read();
                q = ScalarConverter.Read(ref reader, typeof(JsonScalar), options);
            }
            else if (reader.ValueTextEquals("order"u8))
            {
                reader.Read();
                order = ScalarConverter.Read(ref reader, typeof(JsonScalar), options);
            }
            else if (reader.ValueTextEquals("extra"u8))
            {
                reader.Read();
                extra = ReadExtra(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return new ProfileEntryDto
        {
            Id = id,
            IsObject = true,
            Q = q,
            Order = order,
            Extra = extra
        };
    }

    private static List<int?>? ReadExtra(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var ids = new List<int?>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ids.Add(FlexibleIdConverter.ReadId(ref reader));
        }

        return ids;
    }
}
