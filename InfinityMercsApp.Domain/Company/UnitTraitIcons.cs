using System.Text;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Company;

/// <summary>Which trait icons a saved trooper's header shows, read from its characteristics, skills and equipment.</summary>
public sealed partial record UnitTraitIcons(
    bool Regular,
    bool Irregular,
    bool TacticalAwareness,
    bool Cube,
    bool Cube2,
    bool Hackable,
    bool Peripheral)
{
    /// <summary>
    /// The icons the texts call for. A Cube 2.0 or an explicit "no cube" hides the plain cube; a peripheral
    /// unit, Servant or Ancillary shows the peripheral icon.
    /// </summary>
    public static UnitTraitIcons From(string? characteristics, string? skills, string? equipment, bool isPeripheralUnit)
    {
        var text = Simplify(string.Join(" ", characteristics ?? string.Empty, skills ?? string.Empty, equipment ?? string.Empty));
        var cube2 = Cube2Pattern().IsMatch(text);
        var noCube = NoCubePattern().IsMatch(text);

        return new UnitTraitIcons(
            Regular: HasWord(text, "regular"),
            Irregular: HasWord(text, "irregular"),
            TacticalAwareness: TacticalAwarenessPattern().IsMatch(text),
            Cube: !cube2 && !noCube && HasWord(text, "cube"),
            Cube2: cube2,
            Hackable: HackablePattern().IsMatch(text),
            Peripheral: isPeripheralUnit || HasWord(text, "servant") || HasWord(text, "ancillary"));
    }

    private static bool HasWord(string text, string word) =>
        Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);

    // Lower case, with anything but letters, digits and dots turned into single spaces.
    private static string Simplify(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value.ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '.' ? c : ' ');
        }

        return Whitespace().Replace(builder.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"\btactical\s*awareness\b", RegexOptions.IgnoreCase)]
    private static partial Regex TacticalAwarenessPattern();

    [GeneratedRegex(@"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex Cube2Pattern();

    [GeneratedRegex(@"\b(no[\s-]*cube|without[\s-]*cube|cube[\s-]*none)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NoCubePattern();

    [GeneratedRegex(@"\bhackable\b|\bhacking\s*device\b|\bhd\s*\+\b|\bkhd\b|\bevo\s*hd\b", RegexOptions.IgnoreCase)]
    private static partial Regex HackablePattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
