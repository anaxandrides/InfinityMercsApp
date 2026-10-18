using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>
/// Perks that fill a gear slot, by rule 4 of choosing a perk: a role ("Hacker (Role)", "Doctor (ReRoll -3) (Role)")
/// fills the Roles slot, and the 360 Visor and Holomask fill Accessories. Taking one when the slot holds gear overrides
/// it, and the gear is lost with no CR back. The slot stays filled by the perk from then on.
/// </summary>
public static class PerkSlots
{
    /// <summary>What a lost item's transaction is recorded under.</summary>
    public const string LostToPerk = "Overridden by a perk";

    private const string RoleTag = "(Role)";

    private static readonly IReadOnlyList<string> Accessories = ["360 Visor", "Holomask"];

    /// <summary>The slot the perk fills; null for a perk that fills none.</summary>
    public static GearSlot? SlotOf(string? perkName)
    {
        var name = Normalize(perkName);
        if (name.Contains(RoleTag, StringComparison.OrdinalIgnoreCase))
        {
            return GearSlot.Roles;
        }

        return Accessories.Any(accessory => string.Equals(name, accessory, StringComparison.OrdinalIgnoreCase)) ? GearSlot.Accessories : null;
    }

    /// <summary>The slots the trooper's perks fill, each with the perk filling it.</summary>
    public static IReadOnlyDictionary<GearSlot, string> Filled(IEnumerable<string> perkNodeIds)
    {
        var filled = new Dictionary<GearSlot, string>();
        foreach (var pick in perkNodeIds.Select(PerkChoices.PickFor).OfType<PerkPick>())
        {
            if (SlotOf(pick.Name) is { } slot)
            {
                filled.TryAdd(slot, pick.Name);
            }
        }

        return filled;
    }

    // The catalog writes the visor as "360º Visor".
    private static string Normalize(string? perkName) =>
        (perkName ?? string.Empty).Replace("º", string.Empty, StringComparison.Ordinal).Replace("°", string.Empty, StringComparison.Ordinal).Trim();
}
