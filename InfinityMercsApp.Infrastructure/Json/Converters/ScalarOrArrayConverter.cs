using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Dtos;

namespace InfinityMercsApp.Infrastructure.Json.Converters;

internal sealed class ScalarOrArrayConverter : JsonConverter<ScalarOrArrayDto>
{
    private static readonly JsonScalarListConverter ListConverter = new();
    private static readonly JsonScalarConverter ScalarConverter = new();

    public override bool HandleNull => true;

    public override ScalarOrArrayDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return new ScalarOrArrayDto(JsonScalarKind.Other, ListConverter.Read(ref reader, typeof(List<JsonScalar>), options), null);
        }

        var scalar = ScalarConverter.Read(ref reader, typeof(JsonScalar), options);
        return new ScalarOrArrayDto(scalar.Kind, null, scalar.AsString());
    }

    public override void Write(Utf8JsonWriter writer, ScalarOrArrayDto value, JsonSerializerOptions options)
    {
        if (value.Items is not null)
        {
            ListConverter.Write(writer, value.Items, options);
        }
        else if (value.Text is not null)
        {
            writer.WriteStringValue(value.Text);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
