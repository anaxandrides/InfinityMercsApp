namespace InfinityMercsApp.Infrastructure.Models.API.Metadata;

using System.Text.Json.Serialization;

public class Weapon
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("wiki")]
    public string? Wiki { get; set; }

    [JsonPropertyName("ammunition")]
    public int? Ammunition { get; set; }

    [JsonPropertyName("burst")]
    public string? Burst { get; set; }

    [JsonPropertyName("damage")]
    public string? Damage { get; set; }

    [JsonPropertyName("saving")]
    public string? Saving { get; set; }

    [JsonPropertyName("savingNum")]
    public string? SavingNum { get; set; }

    [JsonPropertyName("profile")]
    public string? Profile { get; set; }

    [JsonPropertyName("properties")]
    public List<string>? Properties { get; set; }

    /// <summary>Range bands by name (<c>short</c>, <c>med</c>, <c>long</c>, <c>max</c>...), in payload order.</summary>
    [JsonPropertyName("distance")]
    public Dictionary<string, WeaponDistanceBand?>? Distance { get; set; }
}
