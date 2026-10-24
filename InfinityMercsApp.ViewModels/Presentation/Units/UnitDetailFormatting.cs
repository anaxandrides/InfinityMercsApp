using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Presentation.Text;

namespace InfinityMercsApp.Presentation.Units;

/// <summary>
/// Builds the rich text of the unit-detail display: lieutenant skills highlighted, and entries with a
/// wiki link open it when tapped.
/// </summary>
public static class UnitDetailFormatting
{
    public static readonly Color EquipmentColor = Color.FromArgb("#06B6D4");
    public static readonly Color SkillsColor = Color.FromArgb("#F59E0B");
    public static readonly Color RangedColor = Color.FromArgb("#EF4444");
    public static readonly Color MeleeColor = Color.FromArgb("#22C55E");
    public static readonly Color PeripheralColor = Color.FromArgb("#FACC15");
    /// <summary>The purple the word "Lieutenant" is highlighted in on unit details.</summary>
    internal static readonly Color LieutenantColor = Color.FromArgb("#C084FC");

    /// <summary>"Label: a, b" in <paramref name="textColor"/>, each value linked when the lookup knows it.</summary>
    public static RichText NamedSummary(
        string label,
        IEnumerable<string> values,
        IReadOnlyDictionary<int, string>? nameLookup,
        IReadOnlyDictionary<int, string>? links,
        Color textColor)
    {
        var list = UnitDetailText.DistinctSorted(values);
        var formatted = new List<RichTextRun>();
        formatted.Add(new RichTextRun($"{label}: ", textColor));

        if (list.Count == 0)
        {
            formatted.Add(new RichTextRun("-", textColor));
            return new RichText(formatted);
        }

        for (var i = 0; i < list.Count; i++)
        {
            AppendHighlighted(formatted, list[i], textColor, UnitDetailText.LinkForDisplayName(list[i], nameLookup, links));
            if (i < list.Count - 1)
            {
                formatted.Add(new RichTextRun(", ", textColor));
            }
        }

        return new RichText(formatted);
    }

    /// <summary>One entry per line, or "-".</summary>
    public static RichText Lines(IEnumerable<LinkedText> lines, Color textColor)
    {
        var formatted = new List<RichTextRun>();
        var list = lines.ToList();
        if (list.Count == 0)
        {
            formatted.Add(new RichTextRun("-", textColor));
            return new RichText(formatted);
        }

        for (var i = 0; i < list.Count; i++)
        {
            AppendHighlighted(formatted, list[i].Text, textColor, list[i].Url);
            if (i < list.Count - 1)
            {
                formatted.Add(new RichTextRun(Environment.NewLine, textColor));
            }
        }

        return new RichText(formatted);
    }

    /// <summary>A profile name with any lieutenant mention highlighted.</summary>
    public static RichText Name(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return RichText.Of(string.Empty);
        }

        return new RichText(LieutenantRules.HighlightRuns(name)
            .Select(run => new RichTextRun(run.Text, run.IsLieutenant ? LieutenantColor : null))
            .ToList());
    }

    private static void AppendHighlighted(List<RichTextRun> formatted, string text, Color defaultColor, string? link)
    {
        var linkOrNone = string.IsNullOrWhiteSpace(link) ? null : link;
        foreach (var run in LieutenantRules.HighlightRuns(text))
        {
            formatted.Add(new RichTextRun(run.Text, run.IsLieutenant ? LieutenantColor : defaultColor, linkOrNone));
        }
    }

}
