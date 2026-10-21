namespace InfinityMercsApp.Domain.Captain;

/// <summary>
/// A profile section shown as text, one item per line (weapons, skills or equipment), with "-" for none.
/// </summary>
public static class ProfileText
{
    public const string Empty = "-";

    /// <summary>The section's items: trimmed lines, without blanks and "-" placeholders.</summary>
    public static List<string> Lines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && line != Empty)
            .ToList();
    }

    /// <summary>The items one per line, or "-" when there are none.</summary>
    public static string JoinOrDash(IReadOnlyList<string> items) => items.Count == 0 ? Empty : string.Join(Environment.NewLine, items);

    /// <summary>
    /// The section with <paramref name="additions"/> appended, skipping items it already lists (ignoring case).
    /// With <paramref name="keepTextWhenNothingAdded"/>, a call with no additions returns the text untouched.
    /// </summary>
    public static string AppendDistinct(string? text, IReadOnlyList<string> additions, bool keepTextWhenNothingAdded = false)
    {
        if (keepTextWhenNothingAdded && additions.Count == 0)
        {
            return text ?? Empty;
        }

        var lines = Lines(text);
        var seen = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        foreach (var addition in additions)
        {
            if (seen.Add(addition))
            {
                lines.Add(addition);
            }
        }

        return Join(lines);
    }

    /// <summary>The section with each addition appended as "+ item", for previewing upgrades.</summary>
    public static string AppendMarked(string? text, IReadOnlyList<string> additions)
    {
        var lines = Lines(text);
        lines.AddRange(additions.Select(addition => $"+ {addition}"));
        return Join(lines);
    }

    /// <summary>
    /// Item names with chosen items appended: trims everything, drops blanks and "-", and skips choices
    /// already present (ignoring case).
    /// </summary>
    public static List<string> AppendChoices(IEnumerable<string> existing, params IEnumerable<string?> choices)
    {
        var result = existing.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList();
        foreach (var choice in choices)
        {
            var normalized = choice?.Trim();
            if (string.IsNullOrEmpty(normalized) || normalized == Empty)
            {
                continue;
            }

            if (!result.Any(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    private static string Join(List<string> lines) => lines.Count == 0 ? Empty : string.Join(Environment.NewLine, lines);
}
