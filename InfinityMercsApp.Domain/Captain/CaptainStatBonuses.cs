using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>The stat bonuses an improved captain has bought. Vitality applies to VITA, STR and W alike.</summary>
public sealed record CaptainStatBonuses(int Cc, int Bs, int Ph, int Wip, int Arm, int Bts, int Vitality)
{
    public static CaptainStatBonuses None { get; } = new(0, 0, 0, 0, 0, 0, 0);

    /// <summary>The bonus for a stat; VITA, STR and W share the vitality bonus, and other stats get none.</summary>
    public int For(Stat stat) => stat switch
    {
        Stat.Cc => Cc,
        Stat.Bs => Bs,
        Stat.Ph => Ph,
        Stat.Wip => Wip,
        Stat.Arm => Arm,
        Stat.Bts => Bts,
        Stat.Vita or Stat.Str or Stat.W => Vitality,
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

        return string.Join(" | ", order.Select(key => $"{key} {Apply(values[key], BonusForStatlineKey(key))}"));
    }

    // W is the viewer's wounds column, not a statline key, so on a display statline it takes no bonus.
    private int BonusForStatlineKey(string key) =>
        StatKeys.TryParse(key, out var stat) && stat != Stat.W ? For(stat) : 0;
}
