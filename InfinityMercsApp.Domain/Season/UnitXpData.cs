namespace InfinityMercsApp.Domain.Season;

/// <summary>XP a trooper earned from actions ticked off during the game.</summary>
public sealed class UnitXpData
{
    /// <summary>Two checkboxes, ticked in order.</summary>
    public IList<bool> Assist { get; } = [false, false];
    /// <summary>Three checkboxes, ticked in order.</summary>
    public IList<bool> InflictState { get; } = [false, false, false];
    public bool AttemptButton { get; set; }
    public bool SucceedButton { get; set; }
    public bool ScanEnemy { get; set; }
    public bool ScanEnemyFo { get; set; }
    public bool TagAndBag { get; set; }
    public int DowntimeBonusXp { get; set; }

    public int TotalXp =>
        Assist.Count(b => b) +
        InflictState.Count(b => b) +
        (AttemptButton ? 1 : 0) +
        (SucceedButton ? 1 : 0) +
        (ScanEnemy ? 1 : 0) +
        (ScanEnemyFo ? 1 : 0) +
        (TagAndBag ? MissionExperience.TagAndBagXp : 0) +
        DowntimeBonusXp;
}
