using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Id → name lookups built from an army's <c>filters</c> blob with the legacy lookup builder.
/// </summary>
internal sealed record CorpusLookups(
    IReadOnlyDictionary<int, string> Chars,
    IReadOnlyDictionary<int, string> Skills,
    IReadOnlyDictionary<int, string> Equip,
    IReadOnlyDictionary<int, string> Weapons,
    IReadOnlyDictionary<int, string> Ammo,
    IReadOnlyDictionary<int, string> Peripheral,
    IReadOnlyDictionary<int, LegacyViewerOptionRules.ExtraDefinition> Extras)
{
    private static readonly Dictionary<string, CorpusLookups> Cache = new(StringComparer.Ordinal);

    public static CorpusLookups For(string? armyFiltersJson)
    {
        var key = armyFiltersJson ?? string.Empty;
        lock (Cache)
        {
            if (!Cache.TryGetValue(key, out var lookups))
            {
                lookups = new CorpusLookups(
                    LegacySharedUtilities.BuildIdNameLookup(armyFiltersJson, "chars"),
                    LegacySharedUtilities.BuildIdNameLookup(armyFiltersJson, "skills"),
                    LegacySharedUtilities.BuildIdNameLookup(armyFiltersJson, "equip"),
                    LegacySharedUtilities.BuildIdNameLookup(armyFiltersJson, "weapons"),
                    LegacySharedUtilities.BuildIdNameLookup(armyFiltersJson, "ammunition"),
                    LegacySharedUtilities.BuildIdNameLookup(armyFiltersJson, "peripheral"),
                    LegacyViewerOptionRules.BuildExtrasLookup(armyFiltersJson));
                Cache[key] = lookups;
            }

            return lookups;
        }
    }
}
