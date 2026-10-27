using System.Text.Json;
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
        Converters = { new ArmyFilterCatalogConverter() }
    };

    public static string Write(ArmyDefinition army) => JsonSerializer.Serialize(army, Options);

    /// <summary>The army; throws <see cref="JsonException"/> when <paramref name="json"/> isn't one.</summary>
    public static ArmyDefinition Read(string json) =>
        JsonSerializer.Deserialize<ArmyDefinition>(json, Options) ?? throw new JsonException("The army was null.");
}
