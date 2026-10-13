using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

// Typed shape of the Infinity Army API JSON (https://api.corvusbelli.com/army/units/en/{factionId}) for
// the parts the app reads. Property names match the payload exactly (case-sensitive), and every loosely
// typed value goes through a lenient converter so one odd value never discards a whole unit.

/// <summary><c>units[].profileGroups[]</c></summary>
internal sealed class ProfileGroupDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("isc")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Isc { get; set; }

    [JsonPropertyName("category")]
    public JsonScalar Category { get; set; }

    [JsonPropertyName("profiles")]
    [JsonConverter(typeof(LenientObjectListConverter<ProfileDto>))]
    public List<ProfileDto>? Profiles { get; set; }

    [JsonPropertyName("options")]
    [JsonConverter(typeof(LenientObjectListConverter<ProfileOptionDto>))]
    public List<ProfileOptionDto>? Options { get; set; }
}

/// <summary><c>units[].profileGroups[].profiles[]</c> — a trooper statline.</summary>
internal sealed class ProfileDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("logo")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Logo { get; set; }

    [JsonPropertyName("type")]
    public JsonScalar Type { get; set; }

    [JsonPropertyName("str")]
    public JsonScalar Str { get; set; }

    [JsonPropertyName("move")]
    public ScalarOrArrayDto Move { get; set; }

    [JsonPropertyName("cc")]
    public JsonScalar Cc { get; set; }

    [JsonPropertyName("bs")]
    public JsonScalar Bs { get; set; }

    [JsonPropertyName("ph")]
    public JsonScalar Ph { get; set; }

    [JsonPropertyName("wip")]
    public JsonScalar Wip { get; set; }

    [JsonPropertyName("arm")]
    public JsonScalar Arm { get; set; }

    [JsonPropertyName("bts")]
    public JsonScalar Bts { get; set; }

    [JsonPropertyName("w")]
    public JsonScalar W { get; set; }

    [JsonPropertyName("s")]
    public JsonScalar S { get; set; }

    [JsonPropertyName("ava")]
    public JsonScalar Ava { get; set; }

    [JsonPropertyName("chars")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Chars { get; set; }

    [JsonPropertyName("skills")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Skills { get; set; }

    [JsonPropertyName("equip")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Equip { get; set; }

    [JsonPropertyName("weapons")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Weapons { get; set; }

    [JsonPropertyName("peripheral")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Peripheral { get; set; }

    [JsonPropertyName("ammunition")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Ammunition { get; set; }

    [JsonPropertyName("ammo")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Ammo { get; set; }

    /// <summary>
    /// Anything else on the profile. Only consulted for stat spellings the API does not currently use
    /// (<c>"CC"</c>, <c>"Cc"</c>, or stats nested under <c>stats</c>/<c>attributes</c>/<c>attrs</c>).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>
/// A value that is normally an array of scalars but may be a single string — a profile's <c>move</c>
/// (<c>[10, 5]</c> or <c>"10-5"</c>) or a fireteam's <c>type</c> (<c>["CORE","HARIS"]</c> or <c>"CORE"</c>).
/// </summary>
[JsonConverter(typeof(ScalarOrArrayConverter))]
internal readonly struct ScalarOrArrayDto
{
    public ScalarOrArrayDto(JsonScalarKind kind, List<JsonScalar>? items, string? text)
    {
        Kind = kind;
        Items = items;
        Text = text;
    }

    /// <summary><see cref="JsonScalarKind.Other"/> for an array (see <see cref="Items"/>); otherwise the scalar kind.</summary>
    public JsonScalarKind Kind { get; }

    /// <summary>The array items when the value was an array.</summary>
    public List<JsonScalar>? Items { get; }

    /// <summary>The string when the value was a string.</summary>
    public string? Text { get; }

    public bool IsMissing => Kind == JsonScalarKind.Missing;
}

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

/// <summary><c>units[].profileGroups[].options[]</c> — a loadout.</summary>
internal sealed class ProfileOptionDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("swc")]
    public JsonScalar Swc { get; set; }

    [JsonPropertyName("points")]
    public JsonScalar Points { get; set; }

    [JsonPropertyName("cost")]
    public JsonScalar Cost { get; set; }

    [JsonPropertyName("pts")]
    public JsonScalar Pts { get; set; }

    [JsonPropertyName("minis")]
    public JsonScalar Minis { get; set; }

    [JsonPropertyName("disabled")]
    public JsonScalar Disabled { get; set; }

    [JsonPropertyName("orders")]
    [JsonConverter(typeof(LenientObjectListConverter<OrderDto>))]
    public List<OrderDto>? Orders { get; set; }

    [JsonPropertyName("includes")]
    [JsonConverter(typeof(LenientObjectListConverter<IncludeDto>))]
    public List<IncludeDto>? Includes { get; set; }

    [JsonPropertyName("chars")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Chars { get; set; }

    [JsonPropertyName("skills")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Skills { get; set; }

    [JsonPropertyName("equip")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Equip { get; set; }

    [JsonPropertyName("weapons")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Weapons { get; set; }

    [JsonPropertyName("peripheral")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Peripheral { get; set; }

    [JsonPropertyName("ammunition")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Ammunition { get; set; }

    [JsonPropertyName("ammo")]
    [JsonConverter(typeof(ProfileEntryListConverter))]
    public List<ProfileEntryDto>? Ammo { get; set; }
}

/// <summary><c>options[].orders[]</c></summary>
internal sealed class OrderDto
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Type { get; set; }

    [JsonPropertyName("list")]
    public JsonScalar List { get; set; }

    [JsonPropertyName("total")]
    public JsonScalar Total { get; set; }
}

/// <summary><c>options[].includes[]</c> — a reference to another option whose entries are also granted.</summary>
internal sealed class IncludeDto
{
    [JsonPropertyName("group")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Group { get; set; }

    [JsonPropertyName("option")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Option { get; set; }

    [JsonPropertyName("q")]
    public JsonScalar Q { get; set; }
}

/// <summary><c>units[].filters</c> — the ids of everything a unit can field.</summary>
internal sealed class UnitFiltersDto
{
    [JsonPropertyName("categories")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Categories { get; set; }

    [JsonPropertyName("types")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Types { get; set; }

    [JsonPropertyName("chars")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Chars { get; set; }

    [JsonPropertyName("skills")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Skills { get; set; }

    [JsonPropertyName("equip")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Equip { get; set; }

    [JsonPropertyName("weapons")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Weapons { get; set; }

    [JsonPropertyName("ammunition")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Ammunition { get; set; }
}

/// <summary>An item of an army <c>filters</c> section (<c>skills[]</c>, <c>weapons[]</c>, <c>extras[]</c>...).</summary>
internal sealed class ArmyFilterItemDto
{
    [JsonPropertyName("id")]
    public JsonScalar Id { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Url { get; set; }

    [JsonPropertyName("href")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Href { get; set; }

    [JsonPropertyName("link")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Link { get; set; }

    [JsonPropertyName("wiki")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Wiki { get; set; }

    [JsonPropertyName("web")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Web { get; set; }

    [JsonPropertyName("abbr")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Abbr { get; set; }

    [JsonPropertyName("min")]
    public JsonScalar Min { get; set; }

    [JsonPropertyName("max")]
    public JsonScalar Max { get; set; }

    [JsonPropertyName("mercs")]
    public JsonScalar Mercs { get; set; }

    [JsonPropertyName("specops")]
    public JsonScalar SpecOps { get; set; }
}

/// <summary>An army's <c>fireteamChart</c>.</summary>
internal sealed class FireteamChartDto
{
    [JsonPropertyName("spec")]
    public FireteamSpecDto? Spec { get; set; }

    [JsonPropertyName("teams")]
    [JsonConverter(typeof(LenientObjectListConverter<FireteamTeamDto>))]
    public List<FireteamTeamDto>? Teams { get; set; }
}

/// <summary><c>fireteamChart.spec</c>: fireteam kind → allowed count, in payload order. A non-object reads as empty.</summary>
[JsonConverter(typeof(FireteamSpecConverter))]
internal sealed class FireteamSpecDto
{
    public List<(string Key, JsonScalar Value)> Entries { get; init; } = [];
}

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
            spec.Entries.Add((key, ScalarConverter.Read(ref reader, typeof(JsonScalar), options)));
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

/// <summary><c>fireteamChart.teams[]</c></summary>
internal sealed class FireteamTeamDto
{
    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public ScalarOrArrayDto Type { get; set; }

    [JsonPropertyName("units")]
    [JsonConverter(typeof(LenientObjectListConverter<FireteamUnitDto>))]
    public List<FireteamUnitDto>? Units { get; set; }
}

/// <summary><c>fireteamChart.teams[].units[]</c></summary>
internal sealed class FireteamUnitDto
{
    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Slug { get; set; }

    [JsonPropertyName("comment")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Comment { get; set; }

    [JsonPropertyName("min")]
    public JsonScalar Min { get; set; }

    [JsonPropertyName("max")]
    public JsonScalar Max { get; set; }

    [JsonPropertyName("required")]
    public JsonScalar Required { get; set; }
}

/// <summary>An army <c>filters</c> section; a non-array value reads as an empty section.</summary>
[JsonConverter(typeof(ArmyFilterSectionConverter))]
internal sealed class ArmyFilterSectionDto
{
    public List<ArmyFilterItemDto> Items { get; init; } = [];
}

internal sealed class ArmyFilterSectionConverter : JsonConverter<ArmyFilterSectionDto>
{
    private static readonly LenientObjectListConverter<ArmyFilterItemDto> ListConverter = new();

    public override ArmyFilterSectionDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new() { Items = ListConverter.Read(ref reader, typeof(List<ArmyFilterItemDto>), options) ?? [] };

    public override void Write(Utf8JsonWriter writer, ArmyFilterSectionDto value, JsonSerializerOptions options) =>
        ListConverter.Write(writer, value.Items, options);
}

/// <summary>A whole army payload (<c>/army/units/{id}</c>): its units, unit index, filters and charts.</summary>
internal sealed class ArmyPayloadDto
{
    [JsonPropertyName("version")]
    public JsonScalar Version { get; set; }

    [JsonPropertyName("units")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyUnitDto>))]
    public List<ArmyUnitDto>? Units { get; set; }

    [JsonPropertyName("resume")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyResumeDto>))]
    public List<ArmyResumeDto>? Resume { get; set; }

    [JsonPropertyName("reinforcements")]
    public JsonScalar Reinforcements { get; set; }

    [JsonPropertyName("filters")]
    [JsonConverter(typeof(LenientObjectConverter<Dictionary<string, ArmyFilterSectionDto>>))]
    public Dictionary<string, ArmyFilterSectionDto>? Filters { get; set; }

    [JsonPropertyName("relations")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyRelationDto>))]
    public List<ArmyRelationDto>? Relations { get; set; }

    [JsonPropertyName("specops")]
    [JsonConverter(typeof(LenientObjectConverter<SpecopsDto>))]
    public SpecopsDto? Specops { get; set; }

    [JsonPropertyName("fireteamChart")]
    [JsonConverter(typeof(LenientObjectConverter<FireteamChartDto>))]
    public FireteamChartDto? FireteamChart { get; set; }
}

/// <summary>A unit of an army payload (<c>units[]</c> or <c>specops.units[]</c>).</summary>
internal sealed class ArmyUnitDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("idArmy")]
    public JsonScalar IdArmy { get; set; }

    [JsonPropertyName("canonical")]
    public JsonScalar Canonical { get; set; }

    [JsonPropertyName("isc")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Isc { get; set; }

    [JsonPropertyName("iscAbbr")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? IscAbbr { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Slug { get; set; }

    [JsonPropertyName("profileGroups")]
    [JsonConverter(typeof(LenientObjectListConverter<ProfileGroupDto>))]
    public List<ProfileGroupDto>? ProfileGroups { get; set; }

    [JsonPropertyName("options")]
    [JsonConverter(typeof(LenientObjectListConverter<ProfileOptionDto>))]
    public List<ProfileOptionDto>? Options { get; set; }

    [JsonPropertyName("filters")]
    [JsonConverter(typeof(LenientObjectConverter<UnitFiltersDto>))]
    public UnitFiltersDto? Filters { get; set; }

    [JsonPropertyName("factions")]
    [JsonConverter(typeof(JsonScalarListConverter))]
    public List<JsonScalar>? Factions { get; set; }
}

/// <summary>An entry of an army payload's <c>resume</c> unit index.</summary>
internal sealed class ArmyResumeDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("isc")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Isc { get; set; }

    [JsonPropertyName("idArmy")]
    public JsonScalar IdArmy { get; set; }

    [JsonPropertyName("name")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Slug { get; set; }

    [JsonPropertyName("logo")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Logo { get; set; }

    [JsonPropertyName("type")]
    public JsonScalar Type { get; set; }

    [JsonPropertyName("category")]
    public JsonScalar Category { get; set; }
}

/// <summary>An army <c>relations[]</c> entry: <c>{"units":[{"unit":42}],"min":1,"max":1,"group":false}</c>.</summary>
internal sealed class ArmyRelationDto
{
    [JsonPropertyName("units")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyRelationUnitDto>))]
    public List<ArmyRelationUnitDto>? Units { get; set; }

    [JsonPropertyName("min")]
    public JsonScalar Min { get; set; }

    [JsonPropertyName("max")]
    public JsonScalar Max { get; set; }

    [JsonPropertyName("group")]
    public JsonScalar Group { get; set; }
}

internal sealed class ArmyRelationUnitDto
{
    [JsonPropertyName("unit")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Unit { get; set; }
}

/// <summary>An army's <c>specops</c>: the choices a spec-ops trooper can buy, and the spec-ops units.</summary>
internal sealed class SpecopsDto
{
    [JsonPropertyName("skills")]
    [JsonConverter(typeof(LenientObjectListConverter<SpecopsChoiceDto>))]
    public List<SpecopsChoiceDto>? Skills { get; set; }

    [JsonPropertyName("equip")]
    [JsonConverter(typeof(LenientObjectListConverter<SpecopsChoiceDto>))]
    public List<SpecopsChoiceDto>? Equip { get; set; }

    [JsonPropertyName("weapons")]
    [JsonConverter(typeof(LenientObjectListConverter<SpecopsChoiceDto>))]
    public List<SpecopsChoiceDto>? Weapons { get; set; }

    [JsonPropertyName("units")]
    [JsonConverter(typeof(LenientObjectListConverter<ArmyUnitDto>))]
    public List<ArmyUnitDto>? Units { get; set; }
}

/// <summary>A spec-ops choice: <c>{"exp":8,"id":35,"extras":[6],"skills":[{"id":74,"extras":[304]}]}</c>.</summary>
internal sealed class SpecopsChoiceDto
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleIdConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("exp")]
    public JsonScalar Exp { get; set; }

    [JsonPropertyName("extras")]
    [JsonConverter(typeof(IdListConverter))]
    public List<int>? Extras { get; set; }

    [JsonPropertyName("skills")]
    [JsonConverter(typeof(SpecopsLinkListConverter))]
    public List<SpecopsLinkDto>? Skills { get; set; }

    [JsonPropertyName("equip")]
    [JsonConverter(typeof(SpecopsLinkListConverter))]
    public List<SpecopsLinkDto>? Equip { get; set; }

    [JsonPropertyName("weapons")]
    [JsonConverter(typeof(SpecopsLinkListConverter))]
    public List<SpecopsLinkDto>? Weapons { get; set; }
}

/// <summary>An item a spec-ops choice grants: a bare id or <c>{"id":74,"extras":[304]}</c>.</summary>
internal sealed record SpecopsLinkDto(int Id, List<int> Extras);

/// <summary>
/// Source-generated System.Text.Json metadata for the Infinity Army payloads. Reading through this context
/// needs no reflection, so it is trim- and AOT-safe on iOS and Android.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = false)]
[JsonSerializable(typeof(List<ProfileGroupDto>), TypeInfoPropertyName = "ProfileGroups")]
[JsonSerializable(typeof(UnitFiltersDto), TypeInfoPropertyName = "UnitFilters")]
[JsonSerializable(typeof(Dictionary<string, ArmyFilterSectionDto>), TypeInfoPropertyName = "ArmyFilters")]
[JsonSerializable(typeof(FireteamChartDto), TypeInfoPropertyName = "FireteamChart")]
[JsonSerializable(typeof(FireteamTeamDto))]
[JsonSerializable(typeof(FireteamUnitDto))]
[JsonSerializable(typeof(ArmyFilterItemDto))]
[JsonSerializable(typeof(ProfileDto))]
[JsonSerializable(typeof(ProfileOptionDto))]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(IncludeDto))]
[JsonSerializable(typeof(List<ProfileOptionDto>), TypeInfoPropertyName = "ProfileOptions")]
[JsonSerializable(typeof(List<ArmyRelationDto>), TypeInfoPropertyName = "Relations")]
[JsonSerializable(typeof(SpecopsDto), TypeInfoPropertyName = "Specops")]
[JsonSerializable(typeof(ArmyPayloadDto), TypeInfoPropertyName = "ArmyPayload")]
[JsonSerializable(typeof(ArmyUnitDto))]
[JsonSerializable(typeof(ArmyResumeDto))]
[JsonSerializable(typeof(ArmyRelationUnitDto))]
[JsonSerializable(typeof(SpecopsChoiceDto))]
[JsonSerializable(typeof(JsonScalar))]
internal sealed partial class ArmyJsonContext : JsonSerializerContext
{
}
