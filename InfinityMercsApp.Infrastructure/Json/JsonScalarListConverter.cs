using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

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
