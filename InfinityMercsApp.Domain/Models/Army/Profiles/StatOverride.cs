using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>An augment that sets a stat outright, written like "WIP=12".</summary>
public sealed partial record StatOverride(Stat Stat, int Value)
{
    /// <summary>Reads "KEY=value"; false for anything else, including keys that are not stats.</summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out StatOverride? statOverride)
    {
        statOverride = null;
        var match = OverridePattern().Match(text ?? string.Empty);
        if (!match.Success ||
            !StatKeys.TryParse(match.Groups[1].Value, out var stat) ||
            !int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        statOverride = new StatOverride(stat, value);
        return true;
    }

    [GeneratedRegex(@"^\s*([A-Za-z]+)\s*=\s*(\d+)\s*$")]
    private static partial Regex OverridePattern();
}
