using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

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
