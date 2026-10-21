namespace InfinityMercsApp.Domain.Models.Stores;

/// <summary>
/// A skill armour gives. When the trooper already has the skill (by its name before any brackets),
/// <see cref="WhenHeld"/> replaces it instead, e.g. Infiltration becoming Infiltration (+3).
/// </summary>
public sealed class ArmorSkill
{
    public string Name { get; set; } = string.Empty;

    public string? WhenHeld { get; set; }
}
