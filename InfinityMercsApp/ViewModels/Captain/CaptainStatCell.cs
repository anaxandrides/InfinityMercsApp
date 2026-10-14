using System.Globalization;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.ViewModels.Captain;

/// <summary>A column of the captain's statline: the stat's key and its value after the chosen upgrades.</summary>
public sealed record CaptainStatCell(string Key, string Value, bool IsModified)
{
    /// <summary>
    /// The cell for <paramref name="entry"/> with <paramref name="bonuses"/> added. A value that is not a
    /// number (e.g. "-") is shown as it is.
    /// </summary>
    public static CaptainStatCell For(StatValue entry, CaptainStatBonuses bonuses)
    {
        if (!int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var baseValue))
        {
            return new CaptainStatCell(entry.Stat.Key(), entry.Value, false);
        }

        var value = baseValue + bonuses.For(entry.Stat);
        return new CaptainStatCell(entry.Stat.Key(), value.ToString(CultureInfo.InvariantCulture), value != baseValue);
    }
}
