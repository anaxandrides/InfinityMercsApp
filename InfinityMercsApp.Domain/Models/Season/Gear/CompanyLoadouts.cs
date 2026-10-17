namespace InfinityMercsApp.Domain.Models.Season.Gear;

/// <summary>
/// The season gear of every trooper in a company, by company entry index. Each slot holds one copy of an item;
/// the company may own several copies, so the caller says when a copy is taken from another trooper.
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

    /// <summary>
    /// Puts <paramref name="itemName"/> in the trooper's slot; null empties the slot. With <paramref name="takenFrom"/>,
    /// the copy comes out of that trooper's slot (which may be another of this trooper's); without it, it is a copy no
    /// one carries.
    /// </summary>
    public void Assign(int entryIndex, GearSlot slot, string? itemName, int? takenFrom = null)
    {
        if (!string.IsNullOrWhiteSpace(itemName) && takenFrom is { } giver &&
            Holding(itemName).FirstOrDefault(holding =>
                holding.EntryIndex == giver && holding != new GearHolding(entryIndex, slot) && slot.SharesItemsWith(holding.Slot)) is { } copy)
        {
            For(copy.EntryIndex)[copy.Slot] = null;
        }

        For(entryIndex)[slot] = itemName;
    }

    /// <summary>Every copy of <paramref name="itemName"/> carried, in entry order.</summary>
    public IReadOnlyList<GearHolding> Holding(string itemName) =>
        _loadouts.OrderBy(pair => pair.Key)
            .SelectMany(pair => Enum.GetValues<GearSlot>()
                .Where(slot => string.Equals(pair.Value[slot], itemName, StringComparison.OrdinalIgnoreCase))
                .Select(slot => new GearHolding(pair.Key, slot)))
            .ToList();

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
