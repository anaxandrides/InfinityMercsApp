using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Dtos;

namespace InfinityMercsApp.Infrastructure.Json.Converters;

internal sealed class FireteamSpecConverter : JsonConverter<FireteamSpecDto>
{
    private static readonly JsonScalarConverter ScalarConverter = new();

    public override FireteamSpecDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var spec = new FireteamSpecDto();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return spec;
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var key = reader.GetString() ?? string.Empty;
            reader.Read();
            spec.Entries.Add(new(key, ScalarConverter.Read(ref reader, typeof(JsonScalar), options)));
        }

        return spec;
    }

    public override void Write(Utf8JsonWriter writer, FireteamSpecDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (key, scalar) in value.Entries)
        {
            writer.WritePropertyName(key);
            ScalarConverter.Write(writer, scalar, options);
        }

        writer.WriteEndObject();
    }
}
