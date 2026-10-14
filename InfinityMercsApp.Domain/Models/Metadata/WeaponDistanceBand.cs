namespace InfinityMercsApp.Domain.Models.Metadata;

/// <summary>
/// One range band of a weapon as the API names it (e.g. "short"): its upper bound in cm and the modifier
/// that applies within it.
/// </summary>
public class WeaponDistanceBand
{
    public string Name { get; set; } = string.Empty;

    public int Max { get; set; }

    public string Mod { get; set; } = "0";
}
