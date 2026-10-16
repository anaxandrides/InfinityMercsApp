using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonFile
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentifier { get; set; } = string.Empty;
    public string CompanyFilePath { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    // Set when the season is first opened; null in a file that has not been opened since this was added.
    public SeasonStartingFunds? StartingFunds { get; set; }
    public SeasonMarketplace InitialPurchases { get; set; } = new();
    public List<SeasonRound> Rounds { get; set; } = [];
    public SeasonStatus CurrentStatus { get; set; } = new();
    // Key = SavedCompanyEntry.EntryIndex; value = assigned gear slots for that unit.
    public Dictionary<int, List<SeasonUnitGear>> UnitGear { get; set; } = new();

    // Key = SavedCompanyEntry.EntryIndex; injuries, perks and other changes for that trooper.
    public Dictionary<int, SeasonTrooper> Troopers { get; set; } = new();

    /// <summary>Troopers retired this season, in the order they left; a peripheral leaves with its trooper.</summary>
    public List<RetiredTrooper> Retired { get; set; } = [];

    public bool IsRetired(int entryIndex) => Retired.Any(trooper => trooper.EntryIndex == entryIndex);

    /// <summary>The renown the retired troopers keep for the company.</summary>
    public int RetiredRenown => Retired.Sum(trooper => trooper.Renown);

    /// <summary>The trooper's season record, added when it has none yet.</summary>
    public SeasonTrooper TrooperFor(int entryIndex)
    {
        if (!Troopers.TryGetValue(entryIndex, out var trooper))
        {
            trooper = new SeasonTrooper();
            Troopers[entryIndex] = trooper;
        }

        return trooper;
    }
}
