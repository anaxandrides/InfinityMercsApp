using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

internal sealed class JsonScalarConverter : JsonConverter<JsonScalar>
{
    public override bool HandleNull => true;

    public override JsonScalar Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return JsonScalar.Null;
            case JsonTokenType.Number:
                int? int32 = reader.TryGetInt32(out var intValue) ? intValue : null;
                decimal? @decimal = reader.TryGetDecimal(out var decimalValue) ? decimalValue : null;
                var raw = reader.HasValueSequence
                    ? Encoding.UTF8.GetString(reader.ValueSequence)
                    : Encoding.UTF8.GetString(reader.ValueSpan);
                return JsonScalar.FromNumber(raw, int32, @decimal);
            case JsonTokenType.String:
                return JsonScalar.FromString(reader.GetString());
            case JsonTokenType.True:
                return JsonScalar.FromBool(true);
            case JsonTokenType.False:
                return JsonScalar.FromBool(false);
            default:
                reader.Skip();
                return JsonScalar.Other;
        }
    }

    public override void Write(Utf8JsonWriter writer, JsonScalar value, JsonSerializerOptions options)
    {
        switch (value.Kind)
        {
            case JsonScalarKind.Number:
                writer.WriteRawValue(value.Text!);
                break;
            case JsonScalarKind.String:
                writer.WriteStringValue(value.Text);
                break;
            case JsonScalarKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonScalarKind.False:
                writer.WriteBooleanValue(false);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}
