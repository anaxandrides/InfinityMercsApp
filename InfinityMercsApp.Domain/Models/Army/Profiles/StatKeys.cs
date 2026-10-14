namespace InfinityMercsApp.Domain.Models.Army.Profiles;

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
