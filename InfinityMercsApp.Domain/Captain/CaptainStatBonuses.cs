using System.Globalization;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>The stat bonuses an improved captain has bought. Vitality applies to VITA, STR and W alike.</summary>
public sealed record CaptainStatBonuses(int Cc, int Bs, int Ph, int Wip, int Arm, int Bts, int Vitality)
{
    public static CaptainStatBonuses None { get; } = new(0, 0, 0, 0, 0, 0, 0);

    /// <summary>The bonus for a statline key (CC, BS, PH, WIP, ARM, BTS, VITA/STR/W); 0 for any other key.</summary>
    public int For(string statKey) => statKey.ToUpperInvariant() switch
    {
        "CC" => Cc,
        "BS" => Bs,
        "PH" => Ph,
        "WIP" => Wip,
        "ARM" => Arm,
        "BTS" => Bts,
        "VITA" or "STR" or "W" => Vitality,
        _ => 0
    };

    /// <summary>A stat value with a bonus added; values that are not whole numbers ("-", "T") are returned unchanged.</summary>
    public static string Apply(string value, int bonus)
    {
        if (bonus == 0)
        {
            return value;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? (parsed + bonus).ToString(CultureInfo.InvariantCulture)
            : value;
    }

    /// <summary>
    /// A display statline ("CC 14 | BS 12 | ...") with every bonus applied, re-joined as "KEY value | ...".
    /// Segments without a "KEY value" shape are dropped; when no segment has one the text is returned as-is.
    /// </summary>
    public string ApplyToStatline(string? statline)
    {
        if (string.IsNullOrWhiteSpace(statline))
        {
            return statline ?? string.Empty;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();
        foreach (var part in statline.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var firstSpace = part.IndexOf(' ');
            if (firstSpace <= 0 || firstSpace >= part.Length - 1)
            {
                continue;
            }

            var key = part[..firstSpace].Trim();
            var value = part[(firstSpace + 1)..].Trim();
            if (key.Length == 0 || value.Length == 0)
            {
                continue;
            }

            if (!values.ContainsKey(key))
            {
                order.Add(key);
            }

            values[key] = value;
        }

        if (values.Count == 0)
        {
            return statline;
        }

        return string.Join(" | ", order.Select(key => $"{key} {Apply(values[key], IsBonusKey(key) ? For(key) : 0)}"));
    }

    // Only these keys take bonuses on a display statline (W is the viewer's wounds column, not a statline key).
    private static bool IsBonusKey(string key) =>
        key.ToUpperInvariant() is "CC" or "BS" or "PH" or "WIP" or "ARM" or "BTS" or "VITA" or "STR";
}
