namespace InfinityMercsApp.Domain.Models.Filtering;

/// <summary>
/// The names a unit's visible options and profiles offer, used to populate the filter popup.
/// </summary>
public sealed class UnitFilterOptionSets
{
    public UnitFilterOptionSets(
        ISet<string> characteristics,
        ISet<string> skills,
        ISet<string> equipment,
        ISet<string> weapons,
        ISet<string> ammo)
    {
        Characteristics = characteristics;
        Skills = skills;
        Equipment = equipment;
        Weapons = weapons;
        Ammo = ammo;
    }

    public ISet<string> Characteristics { get; }

    public ISet<string> Skills { get; }

    public ISet<string> Equipment { get; }

    public ISet<string> Weapons { get; }

    public ISet<string> Ammo { get; }

    /// <summary>Five empty sets that treat names differing only in case as the same.</summary>
    public static UnitFilterOptionSets IgnoringCase() => new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>A set's names in alphabetical order, ignoring case.</summary>
    public static List<string> Sorted(IEnumerable<string> names) =>
        names.Order(StringComparer.OrdinalIgnoreCase).ToList();
}
