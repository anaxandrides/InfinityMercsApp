using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.ViewModels;

/// <summary>
/// Text rules for the unit-detail display: summary lines, weapon splitting, wiki links for traits and
/// merging a chosen profile into the unit's shared equipment and skills.
/// </summary>
public static class UnitDetailText
{
    private const string Cube2Pattern = @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b";

    /// <summary>"Label: a, b" with distinct values sorted, or "Label: -".</summary>
    public static string NamedSummary(string label, IEnumerable<string> values)
    {
        var list = DistinctSorted(values);
        return list.Count == 0 ? $"{label}: -" : $"{label}: {string.Join(", ", list)}";
    }

    /// <summary>Distinct non-blank values, sorted case-insensitively.</summary>
    public static List<string> DistinctSorted(IEnumerable<string> values) =>
        values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Distinct non-blank values one per line, or "-".</summary>
    public static string JoinOrDash(IEnumerable<string> values)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return list.Count == 0 ? "-" : string.Join(Environment.NewLine, list);
    }

    /// <summary>Close-combat weapons (CCWs, knives, swords, pistols…) are listed apart from ranged ones.</summary>
    public static bool IsMeleeWeaponName(string weaponName) =>
        Regex.IsMatch(
            weaponName,
            @"\bccw\b|\bda ccw\b|\bap ccw\b|\bknife\b|\bsword\b|\bmonofilament\b|\bviral ccw\b|\bpistols?\b|\bclose combat weapon\b|\bcc\s*weapon\b|\bc\.?\s*c\.?\s*weapon\b|\bpara\s*cc\s*weapon\b",
            RegexOptions.IgnoreCase);

    /// <summary>
    /// A configuration's skills, distinct and sorted. Common spec-ops skills are dropped, except that a
    /// lieutenant skill contributes what it adds (e.g. "+1 Order").
    /// </summary>
    public static List<NamedEntry> ConfigurationSkillEntries(IEnumerable<NamedEntry> rawEntries)
    {
        var normalized = new List<NamedEntry>();
        foreach (var entry in rawEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var skillName = entry.Name.Trim();
            if (!LieutenantRules.IsCommonSpecOpsSkill(skillName))
            {
                normalized.Add(entry with { Name = skillName });
                continue;
            }

            // What the lieutenant skill adds has no wiki entry of its own, so it carries no id.
            if (LieutenantRules.SkillDetail(skillName) is { Length: > 0 } detail)
            {
                normalized.Add(new NamedEntry(0, detail));
            }
        }

        return normalized
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Entries paired with their wiki links, merging repeats and keeping the first link found.</summary>
    public static List<LinkedText> LinkedLines(
        IEnumerable<NamedEntry> entries,
        IReadOnlyDictionary<int, string> links)
    {
        var result = new List<LinkedText>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var url = links.TryGetValue(entry.Id, out var resolvedUrl) ? resolvedUrl : null;
            var existingIndex = result.FindIndex(x => string.Equals(x.Text, entry.Name, StringComparison.OrdinalIgnoreCase));
            if (existingIndex < 0)
            {
                result.Add(new LinkedText(entry.Name, url));
            }
            else if (string.IsNullOrWhiteSpace(result[existingIndex].Url) && !string.IsNullOrWhiteSpace(url))
            {
                result[existingIndex] = result[existingIndex] with { Url = url };
            }
        }

        return result;
    }

    /// <summary>The wiki link for a display name, matching the whole name or the name before " (".</summary>
    public static string? LinkForDisplayName(
        string displayName,
        IReadOnlyDictionary<int, string>? nameLookup,
        IReadOnlyDictionary<int, string>? links)
    {
        if (string.IsNullOrWhiteSpace(displayName) || nameLookup is null || links is null)
        {
            return null;
        }

        var parenIndex = displayName.IndexOf(" (", StringComparison.Ordinal);
        var trimmed = parenIndex > 0 ? displayName[..parenIndex] : displayName;

        IEnumerable<string> candidates = [displayName, trimmed];
        foreach (var candidate in candidates)
        {
            foreach (var (id, name) in nameLookup)
            {
                if (links.TryGetValue(id, out var link) && string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return link;
                }
            }
        }

        return null;
    }

    /// <summary>The first non-blank wiki link whose name matches.</summary>
    public static string? FirstLinkWhere(
        IReadOnlyDictionary<int, string> nameLookup,
        IReadOnlyDictionary<int, string> links,
        Func<string, bool> predicate)
    {
        foreach (var (id, name) in nameLookup)
        {
            if (links.TryGetValue(id, out var url) && !string.IsNullOrWhiteSpace(url) && predicate(name))
            {
                return url;
            }
        }

        return null;
    }

    /// <summary>Lower-case letters, digits and dots, other characters collapsed to single spaces.</summary>
    public static string NormalizeTokenText(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value.ToLowerInvariant())
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '.' ? c : ' ');
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    /// <summary>Names a Cube, but not Cube 2.0.</summary>
    public static bool IsCubeName(string name)
    {
        var normalized = NormalizeTokenText(name);
        return Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase) &&
               !Regex.IsMatch(normalized, Cube2Pattern, RegexOptions.IgnoreCase);
    }

    public static bool IsCube2Name(string name) => Regex.IsMatch(NormalizeTokenText(name), Cube2Pattern, RegexOptions.IgnoreCase);

    /// <summary>
    /// Whether the equipment or skills shown name a hacking device or the Hackable trait
    /// (not "non-hackable").
    /// </summary>
    public static bool ContainsHackable(string? equipmentText, string? skillsText)
    {
        foreach (var line in CompanyProfileTextService.SplitDisplayLine(equipmentText).Concat(CompanyProfileTextService.SplitDisplayLine(skillsText)))
        {
            var normalized = NormalizeTokenText(line);
            if (Regex.IsMatch(normalized, @"\bhacking\s*device\b|\bhd\s*\+\b|\bkhd\b|\bevo\s*hd\b", RegexOptions.IgnoreCase))
            {
                return true;
            }

            if (Regex.IsMatch(normalized, @"\bhackable\b", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(normalized, @"\b(non[\s-]*hackable|not[\s-]*hackable)\b", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Values from a summary line ("Equipment: a, b") merged with a profile's own values, with
    /// lieutenant order bonuses written consistently and a standalone "+N Lt Order" dropped when a
    /// lieutenant skill already lists it. "-" when empty.
    /// </summary>
    public static string MergeSummaryAndUnique(string summaryLine, string uniqueValues)
    {
        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        merged.UnionWith(SummaryValues(summaryLine));
        merged.UnionWith(SummaryValues(uniqueValues));

        return merged.Count == 0
            ? "-"
            : string.Join(", ", NormalizeLtOrderEntries(merged).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>The values after a summary's "Label:" prefix, without "-".</summary>
    public static IEnumerable<string> SummaryValues(string? summaryText)
    {
        if (string.IsNullOrWhiteSpace(summaryText))
        {
            return [];
        }

        var colonIndex = summaryText.IndexOf(':');
        var payload = colonIndex >= 0 && colonIndex < summaryText.Length - 1 ? summaryText[(colonIndex + 1)..] : summaryText;
        return CompanyProfileTextService.SplitDisplayLine(payload).Where(x => !string.Equals(x, "-", StringComparison.Ordinal));
    }

    private static IReadOnlyCollection<string> NormalizeLtOrderEntries(IEnumerable<string> values)
    {
        var normalized = values
            .Select(x => Regex.Replace(
                x,
                @"\blieutenant\b([^\n\r]*\(\s*\+(\d+)\s*)orders?(\s*\))",
                "Lieutenant$1Lt Order$3",
                RegexOptions.IgnoreCase))
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();

        if (normalized.Any(LieutenantRules.MentionsLieutenantOrders))
        {
            for (var i = 0; i < normalized.Count; i++)
            {
                normalized[i] = Regex.Replace(normalized[i], @"\+(\d+)\s*(?:regular\s*)?orders?\b", "+$1 Lt Order", RegexOptions.IgnoreCase);
            }
        }

        var detailedLtBonuses = new HashSet<int>();
        foreach (var value in normalized)
        {
            foreach (Match match in Regex.Matches(
                         value,
                         @"\blieutenant\b[^\n\r]*\(\s*\+(\d+)\s*(?:lt|lieutenant)?\s*orders?\s*\)",
                         RegexOptions.IgnoreCase))
            {
                if (int.TryParse(match.Groups[1].Value, out var parsed))
                {
                    detailedLtBonuses.Add(Math.Max(0, parsed));
                }
            }
        }

        var deduped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in normalized)
        {
            var standalone = Regex.Match(value, @"^\+(\d+)\s*(?:lt|lieutenant)\s*orders?\s*$", RegexOptions.IgnoreCase);
            if (standalone.Success &&
                int.TryParse(standalone.Groups[1].Value, out var bonus) &&
                detailedLtBonuses.Contains(Math.Max(0, bonus)))
            {
                continue;
            }

            deduped.Add(value);
        }

        return deduped;
    }

    /// <summary>"Type - Category", falling back to the ids (or "?") when the filters don't name them.</summary>
    public static string UnitSubtitle(int? type, int? category, IReadOnlyDictionary<int, string> typeLookup, IReadOnlyDictionary<int, string> categoryLookup)
    {
        var typeName = type.HasValue && typeLookup.TryGetValue(type.Value, out var t) ? t : type?.ToString() ?? "?";
        var categoryName = category.HasValue && categoryLookup.TryGetValue(category.Value, out var c) ? c : category?.ToString() ?? "?";
        return $"{typeName} - {categoryName}";
    }

    /// <summary>Upper-case with single spaces, for comparing fireteam unit names.</summary>
    public static string NormalizeFireteamUnitName(string value) => Regex.Replace(value, @"\s+", " ").Trim().ToUpperInvariant();

    /// <summary>
    /// The fireteams of a chart for display. With <paramref name="mercsOnly"/>, only units the faction
    /// can field are listed (by slug or name) and teams left empty are dropped.
    /// </summary>
    public static List<FireteamTeamItem> FireteamTeams(
        FireteamChart? chart,
        bool mercsOnly,
        IReadOnlySet<string> allowedSlugs,
        IReadOnlySet<string> allowedNames)
    {
        var teams = new List<FireteamTeamItem>();
        foreach (var team in (chart ?? FireteamChart.Empty).Teams)
        {
            var unitLimits = team.Units
                .Select(unit => new FireteamUnitLimitItem
                {
                    Name = unit.ViewerName,
                    Slug = unit.Slug ?? string.Empty,
                    Min = (unit.Min ?? 0).ToString(CultureInfo.InvariantCulture),
                    Max = (unit.Max ?? 0).ToString(CultureInfo.InvariantCulture)
                })
                .Where(unit => !mercsOnly ||
                               (!string.IsNullOrWhiteSpace(unit.Slug) && allowedSlugs.Contains(unit.Slug)) ||
                               allowedNames.Contains(NormalizeFireteamUnitName(unit.Name)))
                .ToList();

            if (mercsOnly && unitLimits.Count == 0)
            {
                continue;
            }

            teams.Add(new FireteamTeamItem
            {
                Name = team.ViewerName,
                TeamTypes = string.IsNullOrWhiteSpace(team.TypesDisplay) ? "-" : team.TypesDisplay,
                UnitLimits = unitLimits
            });
        }

        return teams;
    }
}
