using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Lieutenants;

namespace InfinityMercsApp.Services.CompanyProfiles;

/// <summary>
/// Plain-text helpers for unit profiles: splitting and joining display lines, merging skills, and telling melee
/// weapons apart.
/// </summary>
public static class CompanyProfileText
{
    /// <summary>
    /// Returns true when a weapon name should be treated as melee/CC.
    /// </summary>
    public static bool IsMeleeWeaponName(string name)
    {
        return Regex.IsMatch(
            name,
            @"\bccw\b|\bda ccw\b|\bap ccw\b|\bknife\b|\bsword\b|\bmonofilament\b|\bviral ccw\b|\bclose combat weapon\b|\bcc\s*weapon\b|\bc\.?\s*c\.?\s*weapon\b|\bpara\s*cc\s*weapon\b",
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Joins values into display lines, or "-" when empty.
    /// </summary>
    public static string JoinOrDash(IEnumerable<string> values)
    {
        var list = values.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        return list.Count == 0 ? "-" : string.Join(Environment.NewLine, list);
    }

    /// <summary>
    /// Ensures lieutenant skill text exists in the returned sorted list.
    /// </summary>
    public static List<string> EnsureLieutenantSkill(IEnumerable<string> skills) => LieutenantRules.WithSkill(skills);

    /// <summary>
    /// Normalizes raw option skill names into display-ready unique names.
    /// </summary>
    public static List<string> BuildConfigurationSkillNames(IEnumerable<string> rawSkillNames)
    {
        var result = new List<string>();
        foreach (var rawName in rawSkillNames)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                continue;
            }

            var skillName = rawName.Trim();
            if (!LieutenantRules.IsCommonSpecOpsSkill(skillName))
            {
                result.Add(skillName);
                continue;
            }

            // Preserve the base skill context so save conversion can persist id + extra.
            var lieutenantDetail = LieutenantRules.SkillDetail(skillName) is { } detail
                ? $"{LieutenantRules.SkillName} ({detail})"
                : null;
            if (!string.IsNullOrWhiteSpace(lieutenantDetail))
            {
                result.Add(lieutenantDetail);
            }
        }

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Merges common values with a comma-delimited unique-value string.
    /// </summary>
    public static List<string> MergeCommonAndUnique(IEnumerable<string> commonValues, string? uniqueValues)
    {
        var merged = new List<string>();
        foreach (var value in commonValues)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                merged.Add(value.Trim());
            }
        }

        merged.AddRange(SplitDisplayLine(uniqueValues));

        return merged
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Splits display text into normalized item tokens.
    /// </summary>
    public static List<string> SplitDisplayLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "-")
        {
            return [];
        }

        return SplitDisplayTokens(text);
    }

    /// <summary>
    /// Splits display text by line breaks and top-level commas, preserving commas inside parentheses/brackets.
    /// </summary>
    private static List<string> SplitDisplayTokens(string text)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var parenDepth = 0;
        var bracketDepth = 0;

        void FlushCurrent()
        {
            var token = current.ToString().Trim();
            current.Clear();
            if (!string.IsNullOrWhiteSpace(token) && token != "-")
            {
                tokens.Add(token);
            }
        }

        foreach (var ch in text)
        {
            switch (ch)
            {
                case '(':
                    parenDepth++;
                    current.Append(ch);
                    break;
                case ')':
                    if (parenDepth > 0)
                    {
                        parenDepth--;
                    }

                    current.Append(ch);
                    break;
                case '[':
                    bracketDepth++;
                    current.Append(ch);
                    break;
                case ']':
                    if (bracketDepth > 0)
                    {
                        bracketDepth--;
                    }

                    current.Append(ch);
                    break;
                case '\r':
                case '\n':
                    FlushCurrent();
                    break;
                case ',' when parenDepth == 0 && bracketDepth == 0:
                    FlushCurrent();
                    break;
                default:
                    current.Append(ch);
                    break;
            }
        }

        FlushCurrent();
        return tokens;
    }
}
