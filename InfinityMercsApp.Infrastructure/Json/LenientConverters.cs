using System.Buffers;
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

/// <summary>
/// Reads an object with its normal contract; any other value (array, string, number...) reads as <c>null</c>
/// instead of failing the surrounding payload.
/// </summary>
internal sealed class LenientObjectConverter<T> : JsonConverter<T?>
    where T : class
{
    public override bool HandleNull => true;

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return null;
        }

        return JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));
    }
}

/// <summary>
/// An array of ids: numbers, numeric strings and <c>{"id":..}</c> objects, in order; anything else is skipped.
/// A non-array value reads as <c>null</c>.
/// </summary>
internal sealed class IdListConverter : JsonConverter<List<int>?>
{
    public override bool HandleNull => true;

    public override List<int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        var ids = new List<int>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (FlexibleIdConverter.ReadId(ref reader) is int id)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    public override void Write(Utf8JsonWriter writer, List<int>? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var id in value)
        {
            writer.WriteNumberValue(id);
        }

        writer.WriteEndArray();
    }
}

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

/// <summary>A string as sent, or a number's raw text (<c>3</c> → <c>"3"</c>); anything else reads as <c>null</c>.</summary>
internal sealed class ScalarTextConverter : JsonConverter<string?>
{
    public override bool HandleNull => true;

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                return System.Text.Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
