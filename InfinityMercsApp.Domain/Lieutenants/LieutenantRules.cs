using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.Domain.Lieutenants;

/// <summary>
/// Rules for recognising, adding and counting lieutenants in the text-based profile data the company
/// screens work with (skill names, skill lines, saved skill lists).
/// </summary>
public static class LieutenantRules
{
    public const string SkillName = "Lieutenant";

    private const string HighlightToken = "lieutenant";

    /// <summary>True when the text mentions a lieutenant ("Lieutenant", "Lieutenant (+1 Order)"...).</summary>
    public static bool Mentions(string? text) =>
        text?.Contains(HighlightToken, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>True when a skill entry is a lieutenant skill: it mentions a lieutenant or is the abbreviation "Lt".</summary>
    public static bool IsLieutenantSkill(string? skill) =>
        Mentions(skill) || (skill is not null && Regex.IsMatch(skill, @"\bLt\b", RegexOptions.IgnoreCase));

    /// <summary>True when a skill line is about lieutenants or lieutenant orders.</summary>
    public static bool MentionsLieutenantOrders(string? line) =>
        line is not null &&
        (line.Contains("lt order", StringComparison.OrdinalIgnoreCase) || Mentions(line));

    /// <summary>Adds a plain "Lieutenant" skill to <paramref name="skills"/> unless one is already listed.</summary>
    public static void AddSkillIfMissing(List<string> skills)
    {
        if (!skills.Any(IsLieutenantSkill))
        {
            skills.Add(SkillName);
        }
    }

    /// <summary>The skills, distinct and sorted (ignoring case), with "Lieutenant" added when none mentions it.</summary>
    public static List<string> WithSkill(IEnumerable<string> skills)
    {
        var list = skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!list.Any(Mentions))
        {
            list.Add(SkillName);
        }

        return list.OrderBy(skill => skill, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Moves the first skill that mentions a lieutenant to the front.</summary>
    public static void MoveLieutenantFirst(List<string> skills)
    {
        var index = skills.FindIndex(skill => Mentions(skill));
        if (index > 0)
        {
            var lieutenant = skills[index];
            skills.RemoveAt(index);
            skills.Insert(0, lieutenant);
        }
    }

    /// <summary>
    /// Skills every spec-ops trooper shares (lieutenant and the Infinity Spec-Ops skill itself), which the
    /// configuration screens treat specially.
    /// </summary>
    public static bool IsCommonSpecOpsSkill(string? skillName) =>
        !string.IsNullOrWhiteSpace(skillName) &&
        (Mentions(skillName) ||
         skillName.Contains("infinity spec-ops", StringComparison.OrdinalIgnoreCase) ||
         skillName.Contains("infinity spec ops", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// What a composite lieutenant skill adds besides the word "Lieutenant", e.g. "+1 Order" for
    /// "Lieutenant (+1 Order)"; null when the skill is not a lieutenant skill or adds nothing.
    /// </summary>
    public static string? SkillDetail(string skillName)
    {
        if (!Mentions(skillName))
        {
            return null;
        }

        var detail = Regex.Replace(skillName, HighlightToken, string.Empty, RegexOptions.IgnoreCase)
            .Trim()
            .Trim('(', ')', '[', ']', '-', ':', ',', ';', ' ');
        return string.IsNullOrWhiteSpace(detail) ? null : detail;
    }

    /// <summary>
    /// The text split into runs for display, each flagged when it is the word "Lieutenant" (any case),
    /// so screens can highlight it. Empty text gives no runs.
    /// </summary>
    public static IReadOnlyList<TextRun> HighlightRuns(string? text)
    {
        var runs = new List<TextRun>();
        if (string.IsNullOrEmpty(text))
        {
            return runs;
        }

        var start = 0;
        while (true)
        {
            var index = text.IndexOf(HighlightToken, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                if (start < text.Length)
                {
                    runs.Add(TextRun.Plain(text[start..]));
                }

                return runs;
            }

            if (index > start)
            {
                runs.Add(TextRun.Plain(text[start..index]));
            }

            runs.Add(TextRun.Lieutenant(text.Substring(index, HighlightToken.Length)));
            start = index + HighlightToken.Length;
        }
    }

    /// <summary>
    /// The extra lieutenant orders granted by skill lines: standalone "+n Lt Order" lines plus
    /// "Lieutenant (+n ... order)" details.
    /// </summary>
    public static int CountBonusOrders(string? uniqueSkills)
    {
        if (string.IsNullOrWhiteSpace(uniqueSkills))
        {
            return 0;
        }

        var total = 0;
        foreach (var line in ProfileText.Lines(uniqueSkills))
        {
            if (!MentionsLieutenantOrders(line))
            {
                continue;
            }

            var matches = Regex.Matches(
                line,
                @"^\s*\+(\d+)\s*(?:lt|lieutenant)\s*orders?\s*$",
                RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Groups.Count < 2 || !int.TryParse(match.Groups[1].Value, out var value))
                {
                    continue;
                }

                total += Math.Max(0, value);
            }

            var lieutenantDetailMatches = Regex.Matches(
                line,
                @"\blieutenant\b[^\n\r]*\(\s*\+(\d+)\s*(?:(?:lt|lieutenant)\s*)?orders?\s*\)",
                RegexOptions.IgnoreCase);
            foreach (Match match in lieutenantDetailMatches)
            {
                if (match.Groups.Count < 2 || !int.TryParse(match.Groups[1].Value, out var value))
                {
                    continue;
                }

                total += Math.Max(0, value);
            }
        }

        return total;
    }

    /// <summary>
    /// Rewrites "+n order(s)" skill lines as "+n Lt Order" and drops a standalone "+n Lt Order" line when a
    /// Lieutenant line already states the same "(+n ... order)"; duplicate lines are removed. "-" when empty.
    /// </summary>
    public static string NormalizeOrderEntries(string? skillsText)
    {
        var lines = ProfileText.Lines(skillsText);
        if (lines.Count == 0)
        {
            return ProfileText.Empty;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            lines[i] = Regex.Replace(
                lines[i],
                @"\+(\d+)\s*(?:regular\s*)?orders?\b",
                "+$1 Lt Order",
                RegexOptions.IgnoreCase);
        }

        var detailedLtOrderValues = new HashSet<int>();
        foreach (var line in lines)
        {
            var detailMatches = Regex.Matches(
                line,
                @"\blieutenant\b[^\n\r]*\(\s*\+(\d+)\s*(?:lt|lieutenant)?\s*orders?\s*\)",
                RegexOptions.IgnoreCase);
            foreach (Match match in detailMatches)
            {
                if (match.Groups.Count < 2 || !int.TryParse(match.Groups[1].Value, out var value))
                {
                    continue;
                }

                detailedLtOrderValues.Add(Math.Max(0, value));
            }
        }

        var deduped = new List<string>(lines.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var standaloneLtOrderMatch = Regex.Match(
                line,
                @"^\+(\d+)\s*(?:lt|lieutenant)\s*orders?\s*$",
                RegexOptions.IgnoreCase);
            if (standaloneLtOrderMatch.Success &&
                standaloneLtOrderMatch.Groups.Count >= 2 &&
                int.TryParse(standaloneLtOrderMatch.Groups[1].Value, out var standaloneValue) &&
                detailedLtOrderValues.Contains(Math.Max(0, standaloneValue)))
            {
                continue;
            }

            if (seen.Add(line))
            {
                deduped.Add(line);
            }
        }

        return deduped.Count == 0 ? ProfileText.Empty : string.Join(Environment.NewLine, deduped);
    }
}
