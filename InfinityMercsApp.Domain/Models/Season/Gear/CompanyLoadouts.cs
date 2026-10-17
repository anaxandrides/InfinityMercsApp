namespace InfinityMercsApp.Domain.Models.Season.Gear;

/// <summary>
/// The season gear of every trooper in a company, by company entry index. An item can be in one slot at a time
/// (its own kind's or an extra slot): assigning it to another moves it.
/// </summary>
public sealed class CompanyLoadouts
{
    private readonly Dictionary<int, UnitLoadout> _loadouts = [];

    /// <summary>The trooper's loadout, empty when nothing is assigned yet.</summary>
    public UnitLoadout For(int entryIndex)
    {
        if (!_loadouts.TryGetValue(entryIndex, out var loadout))
        {
            loadout = new UnitLoadout();
            _loadouts[entryIndex] = loadout;
        }

        return loadout;
    }

    /// <summary>Puts <paramref name="itemName"/> in the trooper's slot, taking it from whoever had it there; null empties the slot.</summary>
    public void Assign(int entryIndex, GearSlot slot, string? itemName)
    {
        if (!string.IsNullOrWhiteSpace(itemName))
        {
            foreach (var (otherIndex, other) in _loadouts)
            {
                foreach (var otherSlot in Enum.GetValues<GearSlot>().Where(other => slot.SharesItemsWith(other)))
                {
                    if ((otherIndex != entryIndex || otherSlot != slot) && string.Equals(other[otherSlot], itemName, StringComparison.OrdinalIgnoreCase))
                    {
                        other[otherSlot] = null;
                    }
                }
            }
        }

        For(entryIndex)[slot] = itemName;
    }

    /// <summary>
    /// The other trooper holding <paramref name="itemName"/> in that slot or one sharing its items, or null when
    /// no one else does.
    /// </summary>
    public int? HolderOf(string itemName, GearSlot slot, int exceptEntryIndex)
    {
        foreach (var (entryIndex, loadout) in _loadouts)
        {
            if (entryIndex != exceptEntryIndex &&
                Enum.GetValues<GearSlot>().Any(other => slot.SharesItemsWith(other) && string.Equals(loadout[other], itemName, StringComparison.OrdinalIgnoreCase)))
            {
                return entryIndex;
            }
        }

        return null;
    }

    /// <summary>The items assigned to every trooper.</summary>
    public IEnumerable<string> AllItems() => _loadouts.Values.SelectMany(loadout => loadout.Items);

    /// <summary>Every trooper's gear as saved in the season file.</summary>
    public Dictionary<int, List<SeasonUnitGear>> ToSeasonGear() =>
        _loadouts.ToDictionary(pair => pair.Key, pair => pair.Value.ToSeasonGear());

    public static CompanyLoadouts From(IReadOnlyDictionary<int, List<SeasonUnitGear>> unitGear)
    {
        var loadouts = new CompanyLoadouts();
        foreach (var (entryIndex, gear) in unitGear)
        {
            loadouts._loadouts[entryIndex] = UnitLoadout.From(gear);
        }

        return loadouts;
    }
}
