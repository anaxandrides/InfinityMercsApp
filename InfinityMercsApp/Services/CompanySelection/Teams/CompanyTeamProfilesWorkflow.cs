namespace InfinityMercsApp.Views.Common;

internal static class CompanyTeamProfilesWorkflow
{
    internal static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterCharacterUnitLimits<TUnit>(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        TeamUnitResolver<TUnit> resolver,
        Func<TUnit, bool> readIsCharacter)
        where TUnit : class
    {
        var filtered = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            var matchedUnit = resolver.Resolve(unitLimit.Key, unitLimit.Value.Slug);
            if (matchedUnit is not null && readIsCharacter(matchedUnit))
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    internal static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterWildcardUnitLimits(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits)
    {
        var filtered = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            if (CompanyTeamMatchingWorkflow.IsWildcardEntry(unitLimit.Key, unitLimit.Value.Slug))
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    internal static List<TTeamUnitLimitItem> BuildAllowedTeamProfiles<TUnit, TTeamUnitLimitItem>(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        TeamUnitResolver<TUnit> resolver,
        Func<string, string, string, string?, TeamUnitResolver<TUnit>, TTeamUnitLimitItem> buildTeamUnitLimitItem)
        where TUnit : class
        where TTeamUnitLimitItem : class
    {
        var merged = new Dictionary<string, (string Name, int Min, int Max, string? Slug, bool MinAsterisk, bool IsWildcard)>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in unitLimits)
        {
            var name = entry.Key;
            var value = entry.Value;
            var isWildcard = CompanyTeamMatchingWorkflow.IsWildcardEntry(name, value.Slug);
            var key = isWildcard ? "__WILDCARD__" : name.Trim();
            var normalizedName = isWildcard ? "Wildcards" : name;

            if (merged.TryGetValue(key, out var existing))
            {
                merged[key] = (
                    existing.Name,
                    Math.Min(existing.Min, value.Min),
                    Math.Max(existing.Max, value.Max),
                    string.IsNullOrWhiteSpace(existing.Slug) ? value.Slug : existing.Slug,
                    existing.MinAsterisk || value.MinAsterisk,
                    existing.IsWildcard || isWildcard);
                continue;
            }

            merged[key] = (
                normalizedName,
                value.Min,
                value.Max,
                value.Slug,
                value.MinAsterisk,
                isWildcard);
        }

        return merged.Values
            .OrderBy(x => x.IsWildcard ? 1 : 0)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => buildTeamUnitLimitItem(
                x.Name,
                x.MinAsterisk ? "*" : x.Min.ToString(),
                x.Max.ToString(),
                x.Slug,
                resolver))
            .ToList();
    }

    internal static TTeamUnitLimitItem BuildTeamUnitLimitItem<TUnit, TTeamUnitLimitItem>(
        string displayName,
        string min,
        string max,
        string? slug,
        TeamUnitResolver<TUnit> resolver,
        Func<string, string, string, string?, TUnit?, TTeamUnitLimitItem> createItem)
        where TUnit : class
        where TTeamUnitLimitItem : class
    {
        var matched = resolver.Resolve(displayName, slug);
        return createItem(displayName, min, max, slug, matched);
    }

    internal static ArmyTeamUnitLimitItem BuildTeamUnitLimitItem<TUnit>(
        string displayName,
        string min,
        string max,
        string? slug,
        TeamUnitResolver<TUnit> resolver,
        Func<TUnit, bool> readIsCharacter,
        Func<TUnit, string?> readCachedLogoPath,
        Func<TUnit, string?> readPackagedLogoPath,
        Func<TUnit, string?> readSubtitle,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionId)
        where TUnit : class
    {
        var matched = resolver.Resolve(displayName, slug);
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
        TeamUnitResolver<ArmyUnitSelectionItem> resolver)
    {
        return BuildTeamUnitLimitItem<ArmyUnitSelectionItem>(
            displayName,
            min,
            max,
            slug,
            resolver,
            x => x.IsCharacter,
            x => x.CachedLogoPath,
            x => x.PackagedLogoPath,
            x => x.Subtitle,
            x => x.Id,
            x => x.SourceFactionId);
    }
}


