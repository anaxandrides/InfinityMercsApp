using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.Services.CompanySelection.Teams;

/// <summary>The fireteam list of a company selection screen.</summary>
public static class TeamListings
{
    /// <summary>
    /// Each of <paramref name="listedTeams"/> (by name) with its named non-character slots, leaving out those
    /// with none, then one bucket of the wildcard slots of every fireteam in <paramref name="allTeams"/>.
    /// </summary>
    public static IReadOnlyList<TeamListing<TUnit>> Build<TUnit>(
        IReadOnlyList<TUnit> units,
        IEnumerable<MergedFireteam> listedTeams,
        IEnumerable<MergedFireteam> allTeams)
        where TUnit : class, ISelectionUnit
    {
        var listings = new List<TeamListing<TUnit>>();
        foreach (var team in listedTeams.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            // Slots named alike once trimmed share one row, under the first one's name.
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var limits = new Dictionary<string, MergedUnitLimit>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, limit) in WithoutCharacters(team.UnitLimits, units))
            {
                if (FireteamMatching.IsWildcardEntry(name, limit.Slug))
                {
                    continue;
                }

                var key = name.Trim();
                names.TryAdd(key, name);
                limits[key] = limits.TryGetValue(key, out var existing) ? existing.MergeWith(limit) : limit;
            }

            var slots = limits
                .OrderBy(x => names[x.Key], StringComparer.OrdinalIgnoreCase)
                .Select(x => Slot(names[x.Key], x.Value, units))
                .ToList();
            if (slots.Count > 0)
            {
                listings.Add(new TeamListing<TUnit>(team.Name, team, slots));
            }
        }

        var wildcards = new Dictionary<string, MergedUnitLimit>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in allTeams)
        {
            var isWildcardTeam = FireteamMatching.IsWildcardTeamName(team.Name);
            foreach (var (name, limit) in WithoutCharacters(team.UnitLimits, units))
            {
                if (isWildcardTeam || FireteamMatching.IsWildcardEntry(name, limit.Slug))
                {
                    wildcards[name] = wildcards.TryGetValue(name, out var existing) ? existing.MergeWith(limit) : limit;
                }
            }
        }

        var wildcardSlots = wildcards
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => Slot(x.Key, x.Value, units))
            .Where(x => x.Unit is not { IsCharacter: true })
            .ToList();
        if (wildcardSlots.Count > 0)
        {
            listings.Add(new TeamListing<TUnit>(MergedUnitLimit.WildcardsTeamName, null, wildcardSlots));
        }

        return listings;
    }

    // A fireteam's slots without those that match a character.
    private static IEnumerable<KeyValuePair<string, MergedUnitLimit>> WithoutCharacters<TUnit>(
        IReadOnlyDictionary<string, MergedUnitLimit> limits, IReadOnlyList<TUnit> units)
        where TUnit : class, ISelectionUnit =>
        limits.Where(x => Match(x.Key, x.Value.Slug, units) is not { IsCharacter: true });

    private static TeamSlotListing<TUnit> Slot<TUnit>(string name, MergedUnitLimit limit, IReadOnlyList<TUnit> units)
        where TUnit : class, ISelectionUnit =>
        new(name, limit.MinText, limit.MaxText, limit.Slug, Match(name, limit.Slug, units));

    private static TUnit? Match<TUnit>(string name, string? slug, IReadOnlyList<TUnit> units)
        where TUnit : class, ISelectionUnit =>
        FireteamMatching.ResolveUnitForTeamEntry(name, slug, units);
}
