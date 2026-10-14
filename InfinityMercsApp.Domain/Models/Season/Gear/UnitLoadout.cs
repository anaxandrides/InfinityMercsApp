namespace InfinityMercsApp.Domain.Models.Season.Gear;

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
