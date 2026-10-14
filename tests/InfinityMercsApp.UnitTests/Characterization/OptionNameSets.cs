using InfinityMercsApp.Domain.Models.Filtering;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// The five option-name sets the filter popup collects, as concrete HashSets so they can be handed to the frozen
/// legacy code (which takes each set separately) as well as to the domain's <see cref="UnitFilterOptionSets"/>.
/// </summary>
internal sealed class OptionNameSets
{
    public HashSet<string> Characteristics { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Skills { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Equipment { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Weapons { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Ammo { get; } = new(StringComparer.OrdinalIgnoreCase);

    public UnitFilterOptionSets ToDomain() => new(Characteristics, Skills, Equipment, Weapons, Ammo);

    /// <summary>Each set by a readable name, for reporting which one differs.</summary>
    public IReadOnlyDictionary<string, HashSet<string>> ByName => new Dictionary<string, HashSet<string>>
    {
        ["characteristics"] = Characteristics,
        ["skills"] = Skills,
        ["equipment"] = Equipment,
        ["weapons"] = Weapons,
        ["ammo"] = Ammo
    };
}
