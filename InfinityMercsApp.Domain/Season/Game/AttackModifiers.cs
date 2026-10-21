using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// What a trooper's skills and equipment do to its attacks: an X Visor, BS Attack damage reduction, ammo or
/// burst bonuses, and CC Attack damage reduction.
/// </summary>
public sealed partial record AttackModifiers(
    bool HasXVisor,
    int BsDamageReduction,
    string? BsAmmoModifier,
    int BsBurstBonus,
    int CcDamageReduction)
{
    public static AttackModifiers None { get; } = new(false, 0, null, 0, 0);

    /// <summary>The modifiers in <paramref name="skills"/> (as given by <see cref="NormalizeSkills"/>) and <paramref name="equipment"/>.</summary>
    public static AttackModifiers From(string? skills, string? equipment)
    {
        var skillLines = Lines(skills);
        return new AttackModifiers(
            HasXVisor: Lines(skills).Concat(Lines(equipment)).Any(IsXVisor),
            BsDamageReduction: FirstNumber(skillLines, BsDamageReductionPattern()),
            BsAmmoModifier: skillLines.Select(BsAmmoModifierOf).FirstOrDefault(modifier => modifier is not null),
            BsBurstBonus: FirstNumber(skillLines, BsBurstBonusPattern()),
            CcDamageReduction: FirstNumber(skillLines, CcDamageReductionPattern()));
    }

    /// <summary>Rewrites the army builder's "BS Attack (-N SR)" as "BS Attack (SR-N)", for display and reading.</summary>
    public static string NormalizeSkills(string? skills) =>
        string.IsNullOrWhiteSpace(skills) ? string.Empty : LegacyBsAttack().Replace(skills, "$1SR-$2)");

    private static IEnumerable<string> Lines(string? text) =>
        string.IsNullOrWhiteSpace(text) ? [] : text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    private static bool IsXVisor(string line) =>
        line.Contains("X Visor", StringComparison.OrdinalIgnoreCase) || line.Contains("X-Visor", StringComparison.OrdinalIgnoreCase);

    private static int FirstNumber(IEnumerable<string> lines, Regex pattern)
    {
        foreach (var line in lines)
        {
            var match = pattern.Match(line);
            if (match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }

        return 0;
    }

    // "BS Attack (Shock)" gives Shock; damage-reduction and burst forms are read elsewhere.
    private static string? BsAmmoModifierOf(string line)
    {
        var match = BsAttackContent().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var content = match.Groups[1].Value.Trim();
        return NotAnAmmoModifier().IsMatch(content) ? null : content;
    }

    [GeneratedRegex(@"(BS\s+Attack\s*\()-\s*(\d+)\s*SR\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex LegacyBsAttack();

    [GeneratedRegex(@"BS\s+Attack\s*\(\s*(?:SR-|-)\s*(\d+)(?:\s*SR)?\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex BsDamageReductionPattern();

    [GeneratedRegex(@"\bBS\s+Attack\s*\(\s*\+(\d+)\s*B\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex BsBurstBonusPattern();

    [GeneratedRegex(@"CC\s+Attack\s*\(SR-(\d+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex CcDamageReductionPattern();

    [GeneratedRegex(@"\bBS\s+Attack\s*\(([^)]+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex BsAttackContent();

    [GeneratedRegex(@"^SR[-\s]*\d|^-\s*\d.*SR|\+\s*\d+\s*B$", RegexOptions.IgnoreCase)]
    private static partial Regex NotAnAmmoModifier();
}
