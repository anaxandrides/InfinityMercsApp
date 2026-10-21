namespace InfinityMercsApp.Domain.Models.Season.Gear;

public static class GearSlots
{
    /// <summary>The slots that hold weapons.</summary>
    public static IReadOnlyList<GearSlot> Weapons { get; } = [GearSlot.Primary, GearSlot.Secondary, GearSlot.Sidearm, GearSlot.Accessories];

    /// <summary>The kinds of gear the extra slot takes: any but armour, since no one wears two.</summary>
    public static IReadOnlyList<GearSlot> ExtraSlotTakes { get; } =
        [GearSlot.Primary, GearSlot.Secondary, GearSlot.Sidearm, GearSlot.Accessories, GearSlot.Roles, GearSlot.Augments];

    /// <summary>The slot's name as saved and as its inventory category, e.g. "Sidearm".</summary>
    public static string Key(this GearSlot slot) => slot.ToString();

    /// <summary>Reads a slot name, ignoring case.</summary>
    public static bool TryParse(string? key, out GearSlot slot)
    {
        foreach (var candidate in Enum.GetValues<GearSlot>())
        {
            if (string.Equals(candidate.Key(), key?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                slot = candidate;
                return true;
            }
        }

        slot = default;
        return false;
    }

    public static bool HoldsWeapon(this GearSlot slot) => Weapons.Contains(slot);

    /// <summary>Whether an item in one slot could be the same item as one in the other: the same slot, or the extra slot.</summary>
    public static bool SharesItemsWith(this GearSlot slot, GearSlot other) =>
        slot == other || slot == GearSlot.Extra || other == GearSlot.Extra;
}
