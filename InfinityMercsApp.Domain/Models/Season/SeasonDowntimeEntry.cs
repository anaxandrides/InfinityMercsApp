using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonDowntimeEntry
{
    public string EventId { get; set; } = string.Empty;
    public string ChosenPlan { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;

    /// <summary>The participant's company entry; missing in seasons saved before it was recorded, which match by name.</summary>
    public int? ParticipantEntryIndex { get; set; }
    public int CrGain { get; set; }
    public int NotorietyGain { get; set; }
    public int XpGain { get; set; }
    public int SpentCr { get; set; }
    public double SwcGain { get; set; }
    public string OtherEffects { get; set; } = string.Empty;
    public List<SeasonInventoryItem> WonItems { get; set; } = [];
}
