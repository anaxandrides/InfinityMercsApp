namespace InfinityMercsApp.Infrastructure.Models.API.Metadata;

using System.Text.Json.Serialization;

/// <summary>One band of a weapon's <c>distance</c>: <c>{"max":20,"mod":"+3"}</c>.</summary>
public class WeaponDistanceBand
{
    [JsonPropertyName("max")]
    public int Max { get; set; }

    [JsonPropertyName("mod")]
    [JsonConverter(typeof(InfinityMercsApp.Infrastructure.Json.Converters.ScalarTextConverter))]
    public string? Mod { get; set; }
}
