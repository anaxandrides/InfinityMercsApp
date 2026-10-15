namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonMissionUnitResult
{
    /// <summary>The trooper's company entry; missing in seasons saved before it was recorded, which match by name.</summary>
    public int? EntryIndex { get; set; }

    public string UnitName { get; set; } = string.Empty;
    public int XpGained { get; set; }
}
