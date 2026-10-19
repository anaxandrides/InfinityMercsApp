using InfinityMercsApp.Domain.Models.Metadata.Ranges;
using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>
/// A row of the Lootsheet's Weapons tab. <paramref name="RangeMods"/> are the modifiers out to 8", 16", 24", 32",
/// 40", 48" and 96", empty past the weapon's reach; <paramref name="Burst"/> and <paramref name="Ps"/> are empty
/// where the sheet gives none.
/// </summary>
public sealed record LootWeaponRow(
    int Roll,
    string Name,
    LootWeaponKind Kind,
    IReadOnlyList<string> RangeMods,
    IReadOnlyList<string> Traits,
    string Burst,
    string Ps,
    GearSlot Slot,
    decimal Swc)
{
    /// <summary>Where each range column ends, in cm (8" steps, then 96").</summary>
    public static IReadOnlyList<int> RangeColumnEndsCm { get; } = [20, 40, 60, 80, 100, 120, 240];

    public bool HasRangeBands => RangeMods.Any(mod => !string.IsNullOrWhiteSpace(mod));

    /// <summary>The row gives a non-lethal weapon or upgrade, so Continuous can't join it.</summary>
    public bool IsNonLethal => Traits.Any(trait => trait.Contains("Non-Lethal", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The range columns as the army data's bands: neighbouring columns with the same modifier make one band, the
    /// last band is "max" and the ones before it "short", "med" and "long"; null for a weapon without range.
    /// </summary>
    public WeaponDistance? Distance()
    {
        var bands = new List<WeaponDistanceBand>();
        for (var column = 0; column < RangeMods.Count && column < RangeColumnEndsCm.Count; column++)
        {
            var mod = RangeMods[column].Trim();
            if (mod.Length == 0)
            {
                break;
            }

            if (bands.Count > 0 && bands[^1].Mod == mod)
            {
                bands[^1].Max = RangeColumnEndsCm[column];
            }
            else
            {
                bands.Add(new WeaponDistanceBand { Mod = mod, Max = RangeColumnEndsCm[column] });
            }
        }

        if (bands.Count == 0)
        {
            return null;
        }

        IReadOnlyList<string> names = [WeaponDistance.ShortBand, WeaponDistance.MediumBand, WeaponDistance.LongBand];
        for (var index = 0; index < bands.Count; index++)
        {
            bands[index].Name = index == bands.Count - 1 ? WeaponDistance.MaximumBand : names[Math.Min(index, names.Count - 1)];
        }

        return new WeaponDistance { Bands = bands };
    }
}
