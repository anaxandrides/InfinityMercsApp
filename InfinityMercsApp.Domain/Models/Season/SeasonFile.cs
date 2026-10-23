using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>
/// A company's season. A file without its company's name and identifier, or its rounds, isn't read at all, so a save
/// from an app that left them out can't wipe them.
/// </summary>
public class SeasonFile
{
    /// <summary>
    /// The season's own id, which names it online and on the user's other devices; empty in a file from before seasons
    /// were kept online, until it is next saved.
    /// </summary>
    public string SeasonIdentifier { get; set; } = string.Empty;
    [JsonRequired]
    public string CompanyName { get; set; } = string.Empty;
    [JsonRequired]
    public string CompanyIdentifier { get; set; } = string.Empty;
    public string CompanyFilePath { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    // Set when the season is first opened; null in a file that has not been opened since this was added.
    public SeasonStartingFunds? StartingFunds { get; set; }
    public SeasonMarketplace InitialPurchases { get; set; } = new();
    [JsonRequired]
    public List<SeasonRound> Rounds { get; set; } = [];
    public SeasonStatus CurrentStatus { get; set; } = new();
    // Key = SavedCompanyEntry.EntryIndex; value = assigned gear slots for that unit.
    public Dictionary<int, List<SeasonUnitGear>> UnitGear { get; set; } = new();

    // Key = SavedCompanyEntry.EntryIndex; injuries, perks and other changes for that trooper.
    public Dictionary<int, SeasonTrooper> Troopers { get; set; } = new();

    /// <summary>Troopers retired this season, in the order they left; a peripheral leaves with its trooper.</summary>
    public List<RetiredTrooper> Retired { get; set; } = [];

    /// <summary>Loot boxes the company was given, opened or not, in the order they came.</summary>
    public List<SeasonLootBox> LootBoxes { get; set; } = [];

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
