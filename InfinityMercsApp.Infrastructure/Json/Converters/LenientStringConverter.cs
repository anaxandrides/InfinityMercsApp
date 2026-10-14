using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json.Converters;

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
