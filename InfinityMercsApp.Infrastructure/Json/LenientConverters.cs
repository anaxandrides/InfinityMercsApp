using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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

/// <summary>
/// Reads a string property, treating any non-string value as absent instead of failing the whole payload.
/// </summary>
internal sealed class LenientStringConverter : JsonConverter<string?>
{
    public override bool HandleNull => true;

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}

/// <summary>
/// Reads an array of objects, skipping items that are not objects. A non-array value reads as <c>null</c>.
/// </summary>
internal sealed class LenientObjectListConverter<T> : JsonConverter<List<T>?>
    where T : class
{
    public override bool HandleNull => true;

    public override List<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var typeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        var items = new List<T>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                reader.Skip();
                continue;
            }

            var item = JsonSerializer.Deserialize(ref reader, typeInfo);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public override void Write(Utf8JsonWriter writer, List<T>? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var typeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        writer.WriteStartArray();
        foreach (var item in value)
        {
            JsonSerializer.Serialize(writer, item, typeInfo);
        }

        writer.WriteEndArray();
    }
}

/// <summary>
/// Reads an array of scalars (e.g. a move or an id list). A non-array value reads as <c>null</c>.
/// </summary>
internal sealed class JsonScalarListConverter : JsonConverter<List<JsonScalar>?>
{
    private static readonly JsonScalarConverter ItemConverter = new();

    public override bool HandleNull => true;

    public override List<JsonScalar>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var items = new List<JsonScalar>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            items.Add(ItemConverter.Read(ref reader, typeof(JsonScalar), options));
        }

        return items;
    }

    public override void Write(Utf8JsonWriter writer, List<JsonScalar>? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var item in value)
        {
            ItemConverter.Write(writer, item, options);
        }

        writer.WriteEndArray();
    }
}
