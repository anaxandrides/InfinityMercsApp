using InfinityMercsApp.Domain.Lieutenants;
namespace InfinityMercsApp.ViewModels;

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
    public static FormattedString NamedSummary(
        string label,
        IEnumerable<string> values,
        IReadOnlyDictionary<int, string>? nameLookup,
        IReadOnlyDictionary<int, string>? links,
        Color textColor)
    {
        var list = UnitDetailText.DistinctSorted(values);
        var formatted = new FormattedString();
        formatted.Spans.Add(new Span { Text = $"{label}: ", TextColor = textColor });

        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            AppendHighlighted(formatted, list[i], textColor, UnitDetailText.LinkForDisplayName(list[i], nameLookup, links));
            if (i < list.Count - 1)
            {
                formatted.Spans.Add(new Span { Text = ", ", TextColor = textColor });
            }
        }

        return formatted;
    }

    /// <summary>One entry per line, or "-".</summary>
    public static FormattedString Lines(IEnumerable<LinkedText> lines, Color textColor)
    {
        var formatted = new FormattedString();
        var list = lines.ToList();
        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            AppendHighlighted(formatted, list[i].Text, textColor, list[i].Url);
            if (i < list.Count - 1)
            {
                formatted.Spans.Add(new Span { Text = Environment.NewLine, TextColor = textColor });
            }
        }

        return formatted;
    }

    /// <summary>A profile name with any lieutenant mention highlighted.</summary>
    public static FormattedString Name(string name)
    {
        var formatted = new FormattedString();
        if (string.IsNullOrWhiteSpace(name))
        {
            formatted.Spans.Add(new Span { Text = string.Empty });
            return formatted;
        }

        foreach (var run in LieutenantRules.HighlightRuns(name))
        {
            formatted.Spans.Add(run.IsLieutenant ? new Span { Text = run.Text, TextColor = LieutenantColor } : new Span { Text = run.Text });
        }

        return formatted;
    }

    private static void AppendHighlighted(FormattedString formatted, string text, Color defaultColor, string? link)
    {
        foreach (var run in LieutenantRules.HighlightRuns(text))
        {
            var span = new Span { Text = run.Text, TextColor = run.IsLieutenant ? LieutenantColor : defaultColor };
            if (!string.IsNullOrWhiteSpace(link))
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (_, _) => await OpenLinkAsync(link);
                span.GestureRecognizers.Add(tap);
            }

            formatted.Spans.Add(span);
        }
    }

    private static async Task OpenLinkAsync(string link)
    {
        try
        {
            await Launcher.Default.OpenAsync(link);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open link '{link}': {ex.Message}");
        }
    }
}
