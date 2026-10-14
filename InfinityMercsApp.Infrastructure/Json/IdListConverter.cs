using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

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
