using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Models.Army.Filters;

/// <summary>
/// Section names of an army's <c>filters</c> payload: the id → name tables that profile entries refer to.
/// </summary>
public static class ArmyFilterSection
{
    public const string Ammunition = "ammunition";
    public const string Category = "category";
    public const string Chars = "chars";
    public const string Equip = "equip";
    public const string Extras = "extras";
    public const string Peripheral = "peripheral";
    public const string Points = "points";
    public const string Skills = "skills";
    public const string Swc = "swc";
    public const string Type = "type";
    public const string Weapons = "weapons";

    /// <summary>The section a kind of profile entry is named in; ammo entries share the ammunition section.</summary>
    public static string For(ProfileEntryKind kind) => kind switch
    {
        ProfileEntryKind.Chars => Chars,
        ProfileEntryKind.Skills => Skills,
        ProfileEntryKind.Equip => Equip,
        ProfileEntryKind.Weapons => Weapons,
        ProfileEntryKind.Peripheral => Peripheral,
        ProfileEntryKind.Ammunition or ProfileEntryKind.Ammo => Ammunition,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
