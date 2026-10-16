namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>A trooper retired from the company this season.</summary>
public class RetiredTrooper
{
    /// <summary>The trooper's SavedCompanyEntry.EntryIndex; it stays in the company file for AVA.</summary>
    public int EntryIndex { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>The renown it keeps for the company: its cost and XP when it retired, without its returned gear.</summary>
    public int Renown { get; set; }

    /// <summary>How many contracts the season had played when it retired.</summary>
    public int AfterRound { get; set; }
}
