using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

/// <summary>Reads and writes JSON through one converter.</summary>
internal static class ConverterJson
{
    public static T? Read<T>(JsonConverter converter, string json) =>
        JsonSerializer.Deserialize<T>(json, Options(converter));

    public static string Write<T>(JsonConverter converter, T value) =>
        JsonSerializer.Serialize(value, Options(converter));

    private static JsonSerializerOptions Options(JsonConverter converter) => new() { Converters = { converter } };
}
