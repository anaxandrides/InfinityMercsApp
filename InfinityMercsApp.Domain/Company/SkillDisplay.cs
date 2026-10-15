using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.Domain.Company;

/// <summary>
/// A trooper's skills tidied for display: repeats dropped (ignoring case, spacing and the order of
/// arguments) and every Dodge merged into one line at the end.
/// </summary>
public static partial class SkillDisplay
{
    /// <summary>The skills one per line, or "-" when there are none.</summary>
    public static string Normalize(string? skillsText)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dodgeExtras = new List<string>();
        var dodgeExtrasSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasPlainDodge = false;

        foreach (var line in ProfileText.Lines(skillsText))
        {
            var dodge = Dodge().Match(line);
            if (dodge.Success)
            {
                var extra = Whitespace().Replace(dodge.Groups["extra"].Value, " ").Trim();
                if (extra.Length == 0)
                {
                    hasPlainDodge = true;
                }
                else if (dodgeExtrasSeen.Add(extra))
                {
                    dodgeExtras.Add(extra);
                }

                continue;
            }

            if (seen.Add(CanonicalLine(line)))
            {
                result.Add(line);
            }
        }

        var dodgeLines = dodgeExtras.Count > 0
            ? dodgeExtras.Select(extra => $"Dodge ({extra})").ToList()
            : hasPlainDodge ? ["Dodge"] : [];
        result.AddRange(dodgeLines.Where(dodgeLine => seen.Add(CanonicalLine(dodgeLine))));

        return result.Count == 0 ? ProfileText.Empty : string.Join(Environment.NewLine, result);
    }

    /// <summary>A skill line in a form that compares equal however it was spaced or its arguments ordered.</summary>
    public static string CanonicalLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        var normalized = Whitespace().Replace(line.Trim(), " ");
        var match = NameAndArguments().Match(normalized);
        if (!match.Success)
        {
            return normalized;
        }

        var name = match.Groups["name"].Value.Trim();
        var arguments = match.Groups["args"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return arguments.Count == 0 ? name : $"{name} ({string.Join(", ", arguments)})";
    }

    [GeneratedRegex(@"^\s*dodge(?:\s*\((?<extra>.+)\))?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex Dodge();

    [GeneratedRegex(@"^(?<name>[^()]+?)\s*\((?<args>[^()]*)\)\s*$")]
    private static partial Regex NameAndArguments();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
