namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// An extra attached to an entry (e.g. a distance or ammo modifier) as described by the army filters.
/// </summary>
public sealed record ExtraDefinition(string Name, string Type, string? Url)
{
    public bool IsDistance => string.Equals(Type, "DISTANCE", StringComparison.OrdinalIgnoreCase);
}
