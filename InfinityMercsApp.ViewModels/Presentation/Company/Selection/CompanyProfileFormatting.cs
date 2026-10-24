using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Presentation.Text;
using InfinityMercsApp.Presentation.Units;

namespace InfinityMercsApp.Presentation.Company.Selection;

/// <summary>Formatted text for profile and roster lines, with lieutenant mentions highlighted.</summary>
public static class CompanyProfileFormatting
{
    /// <summary>
    /// Builds a formatted name with lieutenant text highlighted.
    /// </summary>
    public static RichText BuildNameFormatted(string name)
    {
        var formatted = new List<RichTextRun>();
        if (string.IsNullOrWhiteSpace(name))
        {
            formatted.Add(new RichTextRun("-"));
            return new RichText(formatted);
        }

        var match = Regex.Match(name, "(lieutenant)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            formatted.Add(new RichTextRun(name));
            return new RichText(formatted);
        }

        if (match.Index > 0)
        {
            formatted.Add(new RichTextRun(name[..match.Index]));
        }

        formatted.Add(new RichTextRun(name.Substring(match.Index, match.Length), UnitDetailFormatting.LieutenantColor));

        var suffixStart = match.Index + match.Length;
        if (suffixStart < name.Length)
        {
            formatted.Add(new RichTextRun(name[suffixStart..]));
        }

        return new RichText(formatted);
    }

    /// <summary>
    /// Builds a multi-line formatted list with optional lieutenant highlighting.
    /// </summary>
    public static RichText BuildListRichText(
        IEnumerable<string> values,
        Color textColor,
        bool highlightLieutenantPurple = false)
    {
        var formatted = new List<RichTextRun>();
        var lines = values.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (lines.Count == 0)
        {
            formatted.Add(new RichTextRun("-", textColor));
            return new RichText(formatted);
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                // A small gap between entries, so the weapons/equipment/skills items don't read as one solid block.
                formatted.Add(RichTextRun.EntryGap);
            }

            var lineColor = highlightLieutenantPurple && LieutenantRules.Mentions(lines[i])
                ? UnitDetailFormatting.LieutenantColor
                : textColor;
            formatted.Add(new RichTextRun(lines[i], lineColor));
        }

        return new RichText(formatted);
    }

    /// <summary>
    /// Builds a labeled, comma-separated summary line.
    /// </summary>
    public static RichText BuildNamedSummaryFormatted(
        string label,
        IEnumerable<string> values,
        Color accentColor,
        bool highlightLieutenantPurple = false)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var formatted = new List<RichTextRun>();
        formatted.Add(new RichTextRun($"{label}: "));
        if (list.Count == 0)
        {
            formatted.Add(new RichTextRun("-"));
            return new RichText(formatted);
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                formatted.Add(new RichTextRun(", "));
            }

            formatted.Add(new RichTextRun(
                list[i],
                highlightLieutenantPurple && LieutenantRules.Mentions(list[i]) ? UnitDetailFormatting.LieutenantColor : accentColor));
        }

        return new RichText(formatted);
    }

    /// <summary>
    /// Builds a single labeled display line for company details.
    /// </summary>
    public static RichText BuildMercsCompanyLineFormatted(string label, string? value, Color accentColor)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "-"
            : value
                .Replace("\r\n", ", ", StringComparison.Ordinal)
                .Replace("\n", ", ", StringComparison.Ordinal)
                .Replace("\r", ", ", StringComparison.Ordinal);

        var formatted = new List<RichTextRun>();
        formatted.Add(new RichTextRun($"{label}: "));
        AppendWithLieutenantHighlight(formatted, normalized, accentColor);
        return new RichText(formatted);
    }

    /// <summary>
    /// Appends text using the default color, but highlights "Lieutenant" in purple.
    /// </summary>
    private static void AppendWithLieutenantHighlight(List<RichTextRun> formatted, string text, Color defaultColor)
    {
        foreach (var run in LieutenantRules.HighlightRuns(text))
        {
            formatted.Add(new RichTextRun(run.Text, run.IsLieutenant ? UnitDetailFormatting.LieutenantColor : defaultColor));
        }
    }
}
