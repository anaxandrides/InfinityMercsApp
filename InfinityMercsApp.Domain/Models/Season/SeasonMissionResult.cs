namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonMissionResult
{
    public int UnitsDeployed { get; set; }
    public bool EliteDeploymentMet { get; set; }
    public int OpponentRp { get; set; }
    public bool Won { get; set; }

    /// <summary>The company could not play this contract and took a bye: its rewards, but no game.</summary>
    public bool IsBye { get; set; }

    public int OpScored { get; set; }
    public int MissionRound { get; set; }
    public List<SeasonMissionUnitResult> UnitResults { get; set; } = [];
}
