using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Season.Inventory;

/// <summary>
/// What a trooper must have to equip an augment, as the market writes it: a skill or piece of equipment
/// ("Engineer"), either of two ("Killer/Hacking Device" is a Killer Hacking Device or a Hacking Device), or a skill
/// it must not have ("Cannot have the BS Attack (+#) skill", where # is any number).
/// </summary>
public sealed partial record AugmentRequirement(string Text)
{
    /// <summary>The requirement written as <paramref name="text"/>; null when the augment has none.</summary>
    public static AugmentRequirement? From(string? text) =>
        string.IsNullOrWhiteSpace(text) || text.Trim() == "-" ? null : new AugmentRequirement(text.Trim());

    /// <summary>How the gear picker notes it on an augment the trooper can't take, e.g. "Needs Engineer".</summary>
    public string Unmet => Forbidden().IsMatch(Text) ? Text : $"Needs {Text}";

    /// <summary>
    /// Whether a trooper holding <paramref name="held"/> (its skills, equipment and carried gear) can take the augment.
    /// A role counts as its skill and the equipment it brings: "Hacker (+Killer Hacking Device)" is both, and a
    /// better device counts as the device ("Hacking Device Plus" is a Hacking Device).
    /// </summary>
    public bool IsMetBy(IEnumerable<string> held)
    {
        var lines = Expand(held).ToList();
        if (Forbidden().Match(Text) is { Success: true } forbidden)
        {
            var pattern = new Regex(
                "^" + Regex.Escape(Normalize(forbidden.Groups["name"].Value)).Replace(@"\#", @"\d+[^)]*", StringComparison.Ordinal) + "$",
                RegexOptions.IgnoreCase);
            return !lines.Any(pattern.IsMatch);
        }

        return Alternatives().Any(name => lines.Any(line => Holds(line, name)));
    }

    // "Killer/Hacking Device" names a Killer Hacking Device and a Hacking Device.
    private IEnumerable<string> Alternatives()
    {
        var parts = Text.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var last = parts[^1];
        return [.. parts[..^1].Select(part => $"{part} {last}"), last];
    }

    // A role's brackets list what it brings: "Engineer (+Gizmokit)".
    private static IEnumerable<string> Expand(IEnumerable<string> held)
    {
        foreach (var line in held.Select(Normalize))
        {
            yield return line;
            if (RoleItems().Match(line) is { Success: true } role)
            {
                foreach (var item in role.Groups["items"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    yield return item;
                }
            }
        }
    }

    private static bool Holds(string line, string name)
    {
        var bracket = line.IndexOf('(', StringComparison.Ordinal);
        var baseName = (bracket < 0 ? line : line[..bracket]).Trim();
        return string.Equals(baseName, name, StringComparison.OrdinalIgnoreCase) ||
               baseName.StartsWith(name + " ", StringComparison.OrdinalIgnoreCase);
    }

    // The army data writes some names with non-breaking spaces.
    private static string Normalize(string text) => text.Replace('\u00A0', ' ').Trim();

    [GeneratedRegex(@"^Cannot\s+have\s+(the\s+)?(?<name>.+?)(\s+skill)?$", RegexOptions.IgnoreCase)]
    private static partial Regex Forbidden();

    [GeneratedRegex(@"\(\+(?<items>[^)]*)\)")]
    private static partial Regex RoleItems();
}
