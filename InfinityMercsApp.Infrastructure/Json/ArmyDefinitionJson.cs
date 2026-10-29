using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// An already-parsed army as JSON and back, without loss: how the API hands the generated companies' armies
/// (Airborne, Inspiring, TAG) to the web site, which imports them as it would a downloaded army.
/// </summary>
public static class ArmyDefinitionJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new ArmyFilterCatalogConverter() },
        TypeInfoResolver = ArmyDefinitionJsonContext.Default
    };

    private static JsonTypeInfo<ArmyDefinition> Army => JsonContracts.For<ArmyDefinition>(Options);

    public static string Write(ArmyDefinition army) => JsonSerializer.Serialize(army, Army);

    /// <summary>The army; throws <see cref="JsonException"/> when <paramref name="json"/> isn't one.</summary>
    public static ArmyDefinition Read(string json) =>
        JsonSerializer.Deserialize(json, Army) ?? throw new JsonException("The army was null.");
}
