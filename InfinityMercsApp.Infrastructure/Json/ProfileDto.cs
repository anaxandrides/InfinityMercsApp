using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

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
