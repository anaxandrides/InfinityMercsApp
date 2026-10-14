namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>The slots a trooper's season gear goes in; each also names the inventory category it is filled from.</summary>
public enum GearSlot
{
    Primary,
    Secondary,
    Sidearm,
    Accessories,
    Roles,
    Armor,
    Augments
}

public static class GearSlots
{
    /// <summary>The slots that hold weapons.</summary>
    public static IReadOnlyList<GearSlot> Weapons { get; } = [GearSlot.Primary, GearSlot.Secondary, GearSlot.Sidearm, GearSlot.Accessories];

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
}

/// <summary>Inventory categories besides the gear slots.</summary>
public static class InventoryCategories
{
    /// <summary>Items exchanged for SWC rather than equipped.</summary>
    public const string Exchange = "Exchange";

    /// <summary>Items with no category.</summary>
    public const string General = "General";

    public static string Normalize(string? category) => string.IsNullOrWhiteSpace(category) ? General : category.Trim();

    /// <summary>Where a category is listed: the gear slots in slot order, then Exchange, then others, General last.</summary>
    public static int Order(string category) => category switch
    {
        _ when GearSlots.TryParse(category, out var slot) && string.Equals(slot.Key(), category, StringComparison.Ordinal) => (int)slot,
        Exchange => 7,
        General => 99,
        _ => 50
    };
}

/// <summary>The season gear assigned to one trooper: at most one item per slot.</summary>
public sealed class UnitLoadout
{
    private readonly Dictionary<GearSlot, string> _items = [];

    /// <summary>The item in a slot; setting a blank name empties the slot.</summary>
    public string? this[GearSlot slot]
    {
        get => _items.GetValueOrDefault(slot);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _items.Remove(slot);
            }
            else
            {
                _items[slot] = value;
            }
        }
    }

    /// <summary>The assigned items in slot order, as saved in the season file.</summary>
    public List<SeasonUnitGear> ToSeasonGear() =>
        Enum.GetValues<GearSlot>()
            .Where(_items.ContainsKey)
            .Select(slot => new SeasonUnitGear { Slot = slot.Key(), ItemName = _items[slot] })
            .ToList();

    /// <summary>A loadout from saved gear; entries for unknown slots are ignored.</summary>
    public static UnitLoadout From(IEnumerable<SeasonUnitGear> gear)
    {
        var loadout = new UnitLoadout();
        foreach (var item in gear)
        {
            if (GearSlots.TryParse(item.Slot, out var slot))
            {
                loadout[slot] = item.ItemName;
            }
        }

        return loadout;
    }
}
