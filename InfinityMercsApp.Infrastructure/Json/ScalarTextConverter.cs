using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

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
                return reader.HasValueSequence
                    ? System.Text.EncodingExtensions.GetString(System.Text.Encoding.UTF8, reader.ValueSequence)
                    : System.Text.Encoding.UTF8.GetString(reader.ValueSpan);
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
