using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonFile
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentifier { get; set; } = string.Empty;
    public string CompanyFilePath { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public SeasonMarketplace InitialPurchases { get; set; } = new();
    public List<SeasonRound> Rounds { get; set; } = [];
    public SeasonStatus CurrentStatus { get; set; } = new();
    // Key = SavedCompanyEntry.EntryIndex; value = assigned gear slots for that unit.
    public Dictionary<int, List<SeasonUnitGear>> UnitGear { get; set; } = new();

    // Key = SavedCompanyEntry.EntryIndex; injuries, perks and other changes for that trooper.
    public Dictionary<int, SeasonTrooper> Troopers { get; set; } = new();

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
