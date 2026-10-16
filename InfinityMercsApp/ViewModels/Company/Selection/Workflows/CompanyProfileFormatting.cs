using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

/// <summary>Formatted text for profile and roster lines, with lieutenant mentions highlighted.</summary>
public static class CompanyProfileFormatting
{
    // Font size of the blank spacer line inserted between profile list entries; small enough that
    // the line reads as a ~2px gap rather than a full empty row.
    private const double EntrySeparatorFontSize = 2d;

    /// <summary>
    /// Builds a formatted name with lieutenant text highlighted.
    /// </summary>
    public static FormattedString BuildNameFormatted(string name)
    {
        var formatted = new FormattedString();
        if (string.IsNullOrWhiteSpace(name))
        {
            formatted.Spans.Add(new Span { Text = "-" });
            return formatted;
        }

        var match = Regex.Match(name, "(lieutenant)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            formatted.Spans.Add(new Span { Text = name });
            return formatted;
        }

        if (match.Index > 0)
        {
            formatted.Spans.Add(new Span { Text = name[..match.Index] });
        }

        formatted.Spans.Add(new Span
        {
            Text = name.Substring(match.Index, match.Length),
            TextColor = UnitDetailFormatting.LieutenantColor
        });

        var suffixStart = match.Index + match.Length;
        if (suffixStart < name.Length)
        {
            formatted.Spans.Add(new Span { Text = name[suffixStart..] });
        }

        return formatted;
    }

    /// <summary>
    /// Builds a multi-line formatted list with optional lieutenant highlighting.
    /// </summary>
    public static FormattedString BuildListFormattedString(
        IEnumerable<string> values,
        Color textColor,
        bool highlightLieutenantPurple = false)
    {
        var formatted = new FormattedString();
        var lines = values.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (lines.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                // A near-zero-height blank line between entries gives ~2px of separation so the
                // weapons/equipment/skills items don't read as one solid block.
                formatted.Spans.Add(new Span { Text = Environment.NewLine + " " + Environment.NewLine, FontSize = EntrySeparatorFontSize, TextColor = textColor });
            }

            var lineColor = highlightLieutenantPurple && LieutenantRules.Mentions(lines[i])
                ? UnitDetailFormatting.LieutenantColor
                : textColor;
            formatted.Spans.Add(new Span { Text = lines[i], TextColor = lineColor });
        }

        return formatted;
    }

    /// <summary>
    /// Builds a labeled, comma-separated summary line.
    /// </summary>
    public static FormattedString BuildNamedSummaryFormatted(
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

        var formatted = new FormattedString();
        formatted.Spans.Add(new Span { Text = $"{label}: " });
        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-" });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                formatted.Spans.Add(new Span { Text = ", " });
            }

            formatted.Spans.Add(new Span
            {
                Text = list[i],
                TextColor = highlightLieutenantPurple && LieutenantRules.Mentions(list[i])
                    ? UnitDetailFormatting.LieutenantColor
                    : accentColor
            });
        }

        return formatted;
    }

    /// <summary>
    /// Builds a single labeled display line for company details.
    /// </summary>
    public static FormattedString BuildMercsCompanyLineFormatted(string label, string? value, Color accentColor)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "-"
            : value
                .Replace("\r\n", ", ", StringComparison.Ordinal)
                .Replace("\n", ", ", StringComparison.Ordinal)
                .Replace("\r", ", ", StringComparison.Ordinal);

        var formatted = new FormattedString();
        formatted.Spans.Add(new Span
        {
            Text = $"{label}: "
        });
        AppendWithLieutenantHighlight(formatted, normalized, accentColor);
        return formatted;
    }

    /// <summary>
    /// Appends text using the default color, but highlights "Lieutenant" in purple.
    /// </summary>
    private static void AppendWithLieutenantHighlight(FormattedString formatted, string text, Color defaultColor)
    {
        foreach (var run in LieutenantRules.HighlightRuns(text))
        {
            formatted.Spans.Add(new Span { Text = run.Text, TextColor = run.IsLieutenant ? UnitDetailFormatting.LieutenantColor : defaultColor });
        }
    }
}
