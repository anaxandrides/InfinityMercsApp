using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

internal static class CompanyTeamProfilesWorkflow
{
    internal static Dictionary<string, MergedUnitLimit> FilterCharacterUnitLimits<TUnit>(
        Dictionary<string, MergedUnitLimit> unitLimits,
        IEnumerable<TUnit> sourceUnits,
        Func<string?, string?, IEnumerable<TUnit>, TUnit?> resolveUnitForTeamEntry,
        Func<TUnit, bool> readIsCharacter)
        where TUnit : class
    {
        var filtered = new Dictionary<string, MergedUnitLimit>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            var matchedUnit = resolveUnitForTeamEntry(unitLimit.Key, unitLimit.Value.Slug, sourceUnits);
            if (matchedUnit is not null && readIsCharacter(matchedUnit))
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    internal static Dictionary<string, MergedUnitLimit> FilterCharacterUnitLimits(
        Dictionary<string, MergedUnitLimit> unitLimits,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits,
        Func<ArmyUnitSelectionItem, bool> readIsCharacter)
    {
        return FilterCharacterUnitLimits(
            unitLimits,
            sourceUnits,
            (allowedProfileName, allowedProfileSlug, units) => FireteamMatching.ResolveUnitForTeamEntry(
                allowedProfileName,
                allowedProfileSlug,
                units,
                x => x.Name,
                x => x.Slug),
            readIsCharacter);
    }

    internal static Dictionary<string, MergedUnitLimit> FilterWildcardUnitLimits(
        Dictionary<string, MergedUnitLimit> unitLimits)
    {
        var filtered = new Dictionary<string, MergedUnitLimit>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            if (FireteamMatching.IsWildcardEntry(unitLimit.Key, unitLimit.Value.Slug))
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    internal static List<TTeamUnitLimitItem> BuildAllowedTeamProfiles<TUnit, TTeamUnitLimitItem>(
        Dictionary<string, MergedUnitLimit> unitLimits,
        IEnumerable<TUnit> sourceUnits,
        Func<string, string, string, string?, IEnumerable<TUnit>, TTeamUnitLimitItem> buildTeamUnitLimitItem)
        where TUnit : class
        where TTeamUnitLimitItem : class
    {
        // Every wildcard slot shares one row, listed after the named units.
        var merged = new Dictionary<string, AllowedProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, limit) in unitLimits)
        {
            var isWildcard = FireteamMatching.IsWildcardEntry(name, limit.Slug);
            var key = isWildcard ? MergedUnitLimit.WildcardsTeamName : name.Trim();
            merged[key] = merged.TryGetValue(key, out var existing)
                ? existing with { Limit = existing.Limit.MergeWith(limit), IsWildcard = existing.IsWildcard || isWildcard }
                : new AllowedProfile(isWildcard ? MergedUnitLimit.WildcardsTeamName : name, limit, isWildcard);
        }

        return merged.Values
            .OrderBy(x => x.IsWildcard ? 1 : 0)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => buildTeamUnitLimitItem(x.Name, x.Limit.MinText, x.Limit.MaxText, x.Limit.Slug, sourceUnits))
            .ToList();
    }

    private sealed record AllowedProfile(string Name, MergedUnitLimit Limit, bool IsWildcard);

    internal static TTeamUnitLimitItem BuildTeamUnitLimitItem<TUnit, TTeamUnitLimitItem>(
        string displayName,
        string min,
        string max,
        string? slug,
        IEnumerable<TUnit> sourceUnits,
        Func<string?, string?, IEnumerable<TUnit>, TUnit?> resolveUnitForTeamEntry,
        Func<string, string, string, string?, TUnit?, TTeamUnitLimitItem> createItem)
        where TUnit : class
        where TTeamUnitLimitItem : class
    {
        var matched = resolveUnitForTeamEntry(displayName, slug, sourceUnits);
        return createItem(displayName, min, max, slug, matched);
    }

    internal static ArmyTeamUnitLimitItem BuildTeamUnitLimitItem<TUnit>(
        string displayName,
        string min,
        string max,
        string? slug,
        IEnumerable<TUnit> sourceUnits,
        Func<string?, string?, IEnumerable<TUnit>, TUnit?> resolveUnitForTeamEntry,
        Func<TUnit, bool> readIsCharacter,
        Func<TUnit, string?> readCachedLogoPath,
        Func<TUnit, string?> readPackagedLogoPath,
        Func<TUnit, string?> readSubtitle,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionId)
        where TUnit : class
    {
        var matched = resolveUnitForTeamEntry(displayName, slug, sourceUnits);
        return new ArmyTeamUnitLimitItem
        {
            Name = displayName,
            Min = min,
            Max = max,
            Slug = slug,
            IsCharacter = matched is not null && readIsCharacter(matched),
            CachedLogoPath = matched is null ? null : readCachedLogoPath(matched),
            PackagedLogoPath = matched is null ? null : readPackagedLogoPath(matched),
            Subtitle = matched is null ? null : readSubtitle(matched),
            ResolvedUnitId = matched is null ? null : readUnitId(matched),
            ResolvedSourceFactionId = matched is null ? null : readSourceFactionId(matched)
        };
    }

    internal static ArmyTeamUnitLimitItem BuildTeamUnitLimitItem(
        string displayName,
        string min,
        string max,
        string? slug,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        return BuildTeamUnitLimitItem<ArmyUnitSelectionItem>(
            displayName,
            min,
            max,
            slug,
            sourceUnits,
            (allowedProfileName, allowedProfileSlug, units) => FireteamMatching.ResolveUnitForTeamEntry(
                allowedProfileName,
                allowedProfileSlug,
                units,
                x => x.Name,
                x => x.Slug),
            x => x.IsCharacter,
            x => x.CachedLogoPath,
            x => x.PackagedLogoPath,
            x => x.Subtitle,
            x => x.Id,
            x => x.SourceFactionId);
    }
}


