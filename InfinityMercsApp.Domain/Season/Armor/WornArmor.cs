using System.Globalization;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.Armor;

/// <summary>
/// A trooper in its armour: the stat line, skills and characteristics the armour gives it, its troop type
/// afterwards, the equipment the armour adds, and any BTS the armour caps.
/// </summary>
public sealed record WornArmor(TrooperProfile Profile, string UnitTypeCode, IReadOnlyList<string> Equipment, int? MaxBts = null)
{
    /// <summary>A trooper wearing no armour that changes it.</summary>
    public static WornArmor None(TrooperProfile profile, string unitTypeCode) => new(profile, unitTypeCode, []);

    /// <summary>
    /// The line after perks, injuries and the like have been added on top of the armour, with the armour's BTS cap
    /// applied: Teseum Plate's BTS can never be raised.
    /// </summary>
    public TrooperProfile Limit(TrooperProfile changed) =>
        MaxBts is { } max && int.TryParse(changed.Bts, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bts) && bts > max
            ? changed with { Bts = max.ToString(CultureInfo.InvariantCulture) }
            : changed;
}
