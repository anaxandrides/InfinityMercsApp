namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>Gear rented with inducements for one contract, in a trooper's slot; what it overrides comes back after.</summary>
public class RentedGear
{
    public string ItemName { get; set; } = string.Empty;

    /// <summary>The slot it fills, as <see cref="Gear.GearSlots.Key"/> names it.</summary>
    public string Slot { get; set; } = string.Empty;

    /// <summary>The trooper carrying it: a company entry, or a hired troop's negative index.</summary>
    public int EntryIndex { get; set; }

    public int Cost { get; set; }
}
