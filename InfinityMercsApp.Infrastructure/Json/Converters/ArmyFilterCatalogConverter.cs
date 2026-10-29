using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using InfinityMercsApp.Domain.Models.Army.Filters;

namespace InfinityMercsApp.Infrastructure.Json.Converters;

/// <summary>
/// Writes an <see cref="ArmyFilterCatalog"/> as its sections and keyed sections, and reads it back, for armies kept as
/// JSON (<see cref="ArmyDefinitionJson"/>); the catalogue holds them privately.
/// </summary>
public sealed class ArmyFilterCatalogConverter : JsonConverter<ArmyFilterCatalog>
{
    public override ArmyFilterCatalog Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stored = JsonSerializer.Deserialize(ref reader, Stored(options))
            ?? throw new JsonException("An army's filters were null.");
        return new ArmyFilterCatalog(stored.Sections, stored.KeyedSections);
    }

    public override void Write(Utf8JsonWriter writer, ArmyFilterCatalog value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(
            writer,
            new StoredArmyFilterCatalog(
                value.Sections.ToDictionary(section => section, value.Items, StringComparer.Ordinal),
                value.KeyedSections.ToDictionary(section => section, value.KeyedItems, StringComparer.Ordinal)),
            Stored(options));

    // The contract from the options' own resolver (ArmyDefinitionJsonContext), so no reflection is needed.
    private static JsonTypeInfo<StoredArmyFilterCatalog> Stored(JsonSerializerOptions options) =>
        JsonContracts.For<StoredArmyFilterCatalog>(options);
}
