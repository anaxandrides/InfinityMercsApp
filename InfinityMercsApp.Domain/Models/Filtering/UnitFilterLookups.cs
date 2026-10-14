using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Models.Filtering;

/// <summary>
/// Id → name lookups the unit filters resolve entry ids against (from the army filters).
/// </summary>
public sealed record UnitFilterLookups(
    IReadOnlyDictionary<int, string> Chars,
    IReadOnlyDictionary<int, string> Skills,
    IReadOnlyDictionary<int, string> Equip,
    IReadOnlyDictionary<int, string> Weapons,
    IReadOnlyDictionary<int, string> Ammo)
{
    private static readonly IReadOnlyDictionary<int, string> EmptyLookup = new Dictionary<int, string>();

    /// <summary>Lookups that only resolve skills (enough for lieutenant checks).</summary>
    public static UnitFilterLookups SkillsOnly(IReadOnlyDictionary<int, string> skills) =>
        new(EmptyLookup, skills, EmptyLookup, EmptyLookup, EmptyLookup);

    public static UnitFilterLookups From(ArmyFilterCatalog? catalog)
    {
        catalog ??= ArmyFilterCatalog.Empty;
        return new(
            catalog.Names(ArmyFilterSection.Chars),
            catalog.Names(ArmyFilterSection.Skills),
            catalog.Names(ArmyFilterSection.Equip),
            catalog.Names(ArmyFilterSection.Weapons),
            catalog.Names(ArmyFilterSection.Ammunition));
    }

    /// <summary>
    /// The lookups of several armies combined: for each id the first army that names it wins, and blank names
    /// are skipped.
    /// </summary>
    public static UnitFilterLookups Merge(IEnumerable<ArmyFilterCatalog> catalogs)
    {
        var all = catalogs.Select(From).ToList();
        return new(
            MergeNames(all.Select(l => l.Chars)),
            MergeNames(all.Select(l => l.Skills)),
            MergeNames(all.Select(l => l.Equip)),
            MergeNames(all.Select(l => l.Weapons)),
            MergeNames(all.Select(l => l.Ammo)));
    }

    /// <summary>Id → name across several lookups; the first non-blank name for an id wins, trimmed.</summary>
    public static IReadOnlyDictionary<int, string> MergeNames(IEnumerable<IReadOnlyDictionary<int, string>> lookups)
    {
        var merged = new Dictionary<int, string>();
        foreach (var (id, name) in lookups.SelectMany(lookup => lookup))
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                merged.TryAdd(id, name.Trim());
            }
        }

        return merged;
    }
}
