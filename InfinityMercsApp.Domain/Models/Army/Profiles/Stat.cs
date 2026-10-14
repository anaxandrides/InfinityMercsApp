using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>A trooper attribute shown on a statline ("MOV 10-5 | CC 14 | BS 12 | ...").</summary>
public enum Stat
{
    Mov,
    Cc,
    Bs,
    Ph,
    Wip,
    Arm,
    Bts,

    /// <summary>Wounds of a living trooper.</summary>
    Vita,

    /// <summary>Structure of a TAG, remote or peripheral; takes VITA's place on the statline.</summary>
    Str,

    /// <summary>The viewer's wounds column, read as VITA.</summary>
    W,

    /// <summary>Silhouette.</summary>
    S,
    Ava
}

/// <summary>One stat as shown on a statline, e.g. MOV "10-5" or CC "14".</summary>
public sealed record StatValue(Stat Stat, string Value);

/// <summary>The unit type codes the rules single out.</summary>
public static class UnitTypeCodes
{
    public const string Tag = "TAG";
    public const string Remote = "REM";
    public const string Peripheral = "PERIPHERAL";

    public static bool IsTag(string? unitTypeCode) => Is(unitTypeCode, Tag);

    /// <summary>TAGs, remotes and peripherals have structure (STR) instead of wounds (VITA).</summary>
    public static bool HasStructure(string? unitTypeCode) =>
        Is(unitTypeCode, Tag) || Is(unitTypeCode, Remote) || Is(unitTypeCode, Peripheral);

    private static bool Is(string? unitTypeCode, string code) =>
        string.Equals(unitTypeCode?.Trim(), code, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Statline keys for <see cref="Stat"/>: <c>Stat.Cc.Key()</c> is "CC", and "cc" parses back to <see cref="Stat.Cc"/>.</summary>
public static class StatKeys
{
    private static readonly IReadOnlyDictionary<Stat, string> KeyByStat = new Dictionary<Stat, string>
    {
        [Stat.Mov] = "MOV",
        [Stat.Cc] = "CC",
        [Stat.Bs] = "BS",
        [Stat.Ph] = "PH",
        [Stat.Wip] = "WIP",
        [Stat.Arm] = "ARM",
        [Stat.Bts] = "BTS",
        [Stat.Vita] = "VITA",
        [Stat.Str] = "STR",
        [Stat.W] = "W",
        [Stat.S] = "S",
        [Stat.Ava] = "AVA"
    };

    private static readonly IReadOnlyDictionary<string, Stat> StatByKey =
        KeyByStat.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>The combat stats a captain can improve and a perk can raise, in statline order.</summary>
    public static IReadOnlyList<Stat> Combat { get; } = [Stat.Cc, Stat.Bs, Stat.Ph, Stat.Wip, Stat.Arm, Stat.Bts];

    /// <summary>The statline key, e.g. "CC".</summary>
    public static string Key(this Stat stat) => KeyByStat[stat];

    /// <summary>Reads a statline key, ignoring case and surrounding spaces.</summary>
    public static bool TryParse(string? key, out Stat stat) => StatByKey.TryGetValue(key?.Trim() ?? string.Empty, out stat);

    /// <summary>VITA, STR and W all count wounds or structure, so a vitality bonus or override applies to each.</summary>
    public static bool IsVitality(this Stat stat) => stat is Stat.Vita or Stat.Str or Stat.W;

    /// <summary>Which wounds column a unit type has: <see cref="Stat.Str"/> for TAGs, remotes and peripherals, else <see cref="Stat.Vita"/>.</summary>
    public static Stat VitalityFor(string? unitTypeCode) => UnitTypeCodes.HasStructure(unitTypeCode) ? Stat.Str : Stat.Vita;
}

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
