using System.Globalization;
using System.Text;

namespace InfinityMercsApp.Views.Common;

internal static class CompanyTeamMatchingWorkflow
{
    internal static bool IsWildcardEntry(string? name, string? slug)
    {
        return ContainsWildcardToken(name) || ContainsWildcardToken(slug);
    }

    internal static bool IsWildcardTeamName(string? teamName)
    {
        return ContainsWildcardToken(teamName);
    }

    internal static bool IsVisibleTeamAllowedProfile<TUnit>(
        string? allowedProfileName,
        string? allowedProfileSlug,
        HashSet<string> visibleUnitNames,
        HashSet<string> visibleUnitSlugs,
        IReadOnlyList<string> visibleNormalizedNames,
        bool treatWildcardAsAlwaysVisible = true,
        IReadOnlyList<TUnit>? visibleUnits = null,
        int? resolvedUnitId = null,
        int? resolvedSourceFactionId = null,
        Func<TUnit, int>? readUnitId = null,
        Func<TUnit, int>? readSourceFactionId = null)
        where TUnit : class
    {
        if (treatWildcardAsAlwaysVisible && IsWildcardEntry(allowedProfileName, allowedProfileSlug))
        {
            return true;
        }

        if (!treatWildcardAsAlwaysVisible)
        {
            if (!resolvedUnitId.HasValue || !resolvedSourceFactionId.HasValue || visibleUnits is null || readUnitId is null || readSourceFactionId is null)
            {
                return false;
            }

            return visibleUnits.Any(x =>
                readUnitId(x) == resolvedUnitId.Value &&
                readSourceFactionId(x) == resolvedSourceFactionId.Value);
        }

        if (resolvedUnitId.HasValue && resolvedSourceFactionId.HasValue && visibleUnits is not null && readUnitId is not null && readSourceFactionId is not null)
        {
            return visibleUnits.Any(x =>
                readUnitId(x) == resolvedUnitId.Value &&
                readSourceFactionId(x) == resolvedSourceFactionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(allowedProfileSlug) &&
            (visibleUnitSlugs.Contains(allowedProfileSlug.Trim()) ||
             ContainsEquivalentSlug(visibleUnitSlugs, allowedProfileSlug)))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(allowedProfileName))
        {
            return false;
        }

        if (visibleUnitNames.Contains(allowedProfileName))
        {
            return true;
        }

        var normalizedAllowed = NormalizeTeamUnitName(allowedProfileName);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return false;
        }

        foreach (var normalizedVisible in visibleNormalizedNames)
        {
            if (string.Equals(normalizedAllowed, normalizedVisible, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static TUnit? ResolveUnitForTeamEntry<TUnit>(
        string? allowedProfileName,
        string? allowedProfileSlug,
        IEnumerable<TUnit> sourceUnits,
        Func<TUnit, string?> readName,
        Func<TUnit, string?> readSlug)
        where TUnit : class
    {
        var reinforcementOnly = IsReinforcementTeamEntry(allowedProfileName);
        var preferredSourceUnits = reinforcementOnly
            ? sourceUnits.Where(x => IsReinforcementUnit(readName(x), readSlug(x))).ToList()
            : sourceUnits.ToList();
        var fallbackSourceUnits = reinforcementOnly
            ? sourceUnits.Where(x => !IsReinforcementUnit(readName(x), readSlug(x))).ToList()
            : [];

        if (!string.IsNullOrWhiteSpace(allowedProfileSlug))
        {
            var slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(readSlug(x)) &&
                string.Equals(readSlug(x)?.Trim(), allowedProfileSlug.Trim(), StringComparison.Ordinal));
            if (slugMatch is not null)
            {
                return slugMatch;
            }

            slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(readSlug(x)) &&
                string.Equals(readSlug(x)?.Trim(), allowedProfileSlug.Trim(), StringComparison.OrdinalIgnoreCase));
            if (slugMatch is not null)
            {
                return slugMatch;
            }

            var normalizedAllowedSlug = NormalizeSlugForLookup(allowedProfileSlug);
            if (!string.IsNullOrWhiteSpace(normalizedAllowedSlug))
            {
                slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(readSlug(x)) &&
                    string.Equals(NormalizeSlugForLookup(readSlug(x)), normalizedAllowedSlug, StringComparison.Ordinal));
                if (slugMatch is not null)
                {
                    return slugMatch;
                }
            }

            if (!string.IsNullOrWhiteSpace(allowedProfileName))
            {
                var slugFallbackNameMatch = preferredSourceUnits.FirstOrDefault(x =>
                    string.Equals(readName(x), allowedProfileName, StringComparison.OrdinalIgnoreCase));
                if (slugFallbackNameMatch is not null)
                {
                    return slugFallbackNameMatch;
                }
            }

            if (reinforcementOnly)
            {
                slugMatch = fallbackSourceUnits.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(readSlug(x)) &&
                    string.Equals(readSlug(x)?.Trim(), allowedProfileSlug.Trim(), StringComparison.OrdinalIgnoreCase));
                if (slugMatch is not null)
                {
                    return slugMatch;
                }
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(allowedProfileName))
        {
            return null;
        }

        var exactNameMatch = preferredSourceUnits.FirstOrDefault(x =>
            string.Equals(readName(x), allowedProfileName, StringComparison.OrdinalIgnoreCase));
        if (exactNameMatch is not null)
        {
            return exactNameMatch;
        }

        var normalizedAllowed = NormalizeTeamUnitName(allowedProfileName);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return null;
        }

        foreach (var unit in preferredSourceUnits)
        {
            var normalizedUnit = NormalizeTeamUnitName(readName(unit));
            if (string.IsNullOrWhiteSpace(normalizedUnit))
            {
                continue;
            }

            if (string.Equals(normalizedAllowed, normalizedUnit, StringComparison.Ordinal))
            {
                return unit;
            }
        }

        if (reinforcementOnly)
        {
            var fallbackNameMatch = fallbackSourceUnits.FirstOrDefault(x =>
                string.Equals(readName(x), allowedProfileName, StringComparison.OrdinalIgnoreCase));
            if (fallbackNameMatch is not null)
            {
                return fallbackNameMatch;
            }
        }

        return null;
    }

    internal static bool ContainsEquivalentSlug(HashSet<string> visibleUnitSlugs, string allowedProfileSlug)
    {
        var normalizedAllowed = NormalizeSlugForLookup(allowedProfileSlug);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return false;
        }

        foreach (var visibleSlug in visibleUnitSlugs)
        {
            if (string.Equals(NormalizeSlugForLookup(visibleSlug), normalizedAllowed, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeTeamUnitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var formD = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToUpperInvariant(c));
            }
        }

        return builder.ToString();
    }

    internal static string NormalizeSlugForLookup(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        var lowered = slug.Trim().ToLowerInvariant().Replace('_', '-');
        var builder = new StringBuilder(lowered.Length);
        var previousWasDash = false;
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                previousWasDash = false;
            }
            else if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        var normalized = builder.ToString().Trim('-');
        if (normalized.StartsWith("reinf-", StringComparison.Ordinal))
        {
            normalized = normalized["reinf-".Length..];
        }

        if (normalized.EndsWith("-reinf", StringComparison.Ordinal))
        {
            normalized = normalized[..^"-reinf".Length];
        }

        return normalized;
    }

    internal static bool IsReinforcementTeamEntry(string? allowedProfileName)
    {
        return !string.IsNullOrWhiteSpace(allowedProfileName) &&
               allowedProfileName.IndexOf("REINF", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool IsReinforcementUnit(string? unitName, string? unitSlug)
    {
        return (!string.IsNullOrWhiteSpace(unitName) &&
                unitName.IndexOf("REINF", StringComparison.OrdinalIgnoreCase) >= 0) ||
               (!string.IsNullOrWhiteSpace(unitSlug) &&
                unitSlug.IndexOf("reinf", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool ContainsWildcardToken(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               (value.IndexOf("wildcard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("wild", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}

/// <summary>
/// Precomputes the lookup tiers <see cref="CompanyTeamMatchingWorkflow.ResolveUnitForTeamEntry{TUnit}"/>
/// scans linearly, so a fixed <c>sourceUnits</c> set can be resolved against repeatedly (e.g. once per
/// team unit-limit) in O(1) instead of O(units) per call. Mirrors that method's tier order, first-match-
/// wins semantics, and reinforcement preferred/fallback partitioning exactly.
/// </summary>
public sealed class TeamUnitResolver<TUnit>
    where TUnit : class
{
    private sealed class Partition
    {
        internal Dictionary<string, TUnit> BySlugOrdinal { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, TUnit> BySlugOrdinalIgnoreCase { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, TUnit> ByNormalizedSlug { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, TUnit> ByNameOrdinalIgnoreCase { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, TUnit> ByNormalizedName { get; } = new(StringComparer.Ordinal);

        internal void Add(TUnit unit, string? name, string? slug)
        {
            if (!string.IsNullOrWhiteSpace(slug))
            {
                var trimmedSlug = slug.Trim();
                BySlugOrdinal.TryAdd(trimmedSlug, unit);
                BySlugOrdinalIgnoreCase.TryAdd(trimmedSlug, unit);

                var normalizedSlug = CompanyTeamMatchingWorkflow.NormalizeSlugForLookup(slug);
                if (!string.IsNullOrWhiteSpace(normalizedSlug))
                {
                    ByNormalizedSlug.TryAdd(normalizedSlug, unit);
                }
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                ByNameOrdinalIgnoreCase.TryAdd(name, unit);

                var normalizedName = CompanyTeamMatchingWorkflow.NormalizeTeamUnitName(name);
                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    ByNormalizedName.TryAdd(normalizedName, unit);
                }
            }
        }
    }

    private sealed class FallbackPartition
    {
        internal Dictionary<string, TUnit> BySlugOrdinalIgnoreCase { get; } = new(StringComparer.OrdinalIgnoreCase);
        internal Dictionary<string, TUnit> ByNameOrdinalIgnoreCase { get; } = new(StringComparer.OrdinalIgnoreCase);

        internal void Add(TUnit unit, string? name, string? slug)
        {
            if (!string.IsNullOrWhiteSpace(slug))
            {
                BySlugOrdinalIgnoreCase.TryAdd(slug.Trim(), unit);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                ByNameOrdinalIgnoreCase.TryAdd(name, unit);
            }
        }
    }

    private readonly Partition _all = new();
    private readonly Partition _reinforcement = new();
    private readonly FallbackPartition _nonReinforcement = new();

    private TeamUnitResolver()
    {
    }

    internal static TeamUnitResolver<TUnit> Build(
        IEnumerable<TUnit> sourceUnits,
        Func<TUnit, string?> readName,
        Func<TUnit, string?> readSlug)
    {
        var resolver = new TeamUnitResolver<TUnit>();
        foreach (var unit in sourceUnits)
        {
            var name = readName(unit);
            var slug = readSlug(unit);
            resolver._all.Add(unit, name, slug);

            if (CompanyTeamMatchingWorkflow.IsReinforcementUnit(name, slug))
            {
                resolver._reinforcement.Add(unit, name, slug);
            }
            else
            {
                resolver._nonReinforcement.Add(unit, name, slug);
            }
        }

        return resolver;
    }

    internal TUnit? Resolve(string? allowedProfileName, string? allowedProfileSlug)
    {
        var reinforcementOnly = CompanyTeamMatchingWorkflow.IsReinforcementTeamEntry(allowedProfileName);
        var preferred = reinforcementOnly ? _reinforcement : _all;

        if (!string.IsNullOrWhiteSpace(allowedProfileSlug))
        {
            var trimmedSlug = allowedProfileSlug.Trim();

            if (preferred.BySlugOrdinal.TryGetValue(trimmedSlug, out var slugExact))
            {
                return slugExact;
            }

            if (preferred.BySlugOrdinalIgnoreCase.TryGetValue(trimmedSlug, out var slugIgnoreCase))
            {
                return slugIgnoreCase;
            }

            var normalizedAllowedSlug = CompanyTeamMatchingWorkflow.NormalizeSlugForLookup(allowedProfileSlug);
            if (!string.IsNullOrWhiteSpace(normalizedAllowedSlug) &&
                preferred.ByNormalizedSlug.TryGetValue(normalizedAllowedSlug, out var slugNormalized))
            {
                return slugNormalized;
            }

            if (!string.IsNullOrWhiteSpace(allowedProfileName) &&
                preferred.ByNameOrdinalIgnoreCase.TryGetValue(allowedProfileName, out var slugFallbackName))
            {
                return slugFallbackName;
            }

            if (reinforcementOnly && _nonReinforcement.BySlugOrdinalIgnoreCase.TryGetValue(trimmedSlug, out var fallbackSlug))
            {
                return fallbackSlug;
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(allowedProfileName))
        {
            return null;
        }

        if (preferred.ByNameOrdinalIgnoreCase.TryGetValue(allowedProfileName, out var nameExact))
        {
            return nameExact;
        }

        var normalizedAllowedName = CompanyTeamMatchingWorkflow.NormalizeTeamUnitName(allowedProfileName);
        if (string.IsNullOrWhiteSpace(normalizedAllowedName))
        {
            return null;
        }

        if (preferred.ByNormalizedName.TryGetValue(normalizedAllowedName, out var nameNormalized))
        {
            return nameNormalized;
        }

        if (reinforcementOnly && _nonReinforcement.ByNameOrdinalIgnoreCase.TryGetValue(allowedProfileName, out var fallbackName))
        {
            return fallbackName;
        }

        return null;
    }
}


