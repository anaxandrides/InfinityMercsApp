using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Web.Presentation;

/// <summary>One contract of a season: its result and the XP it gave.</summary>
/// <param name="Result">"Won", "Lost" or "Bye".</param>
public sealed record ContractRow(int Number, string Result, int ObjectivePoints, int Deployed, int Experience)
{
    public static ContractRow From(SeasonRound round) => new(
        round.RoundIndex,
        round.MissionResults switch
        {
            { IsBye: true } => "Bye",
            { Won: true } => "Won",
            _ => "Lost"
        },
        round.MissionResults.OpScored,
        round.MissionResults.UnitsDeployed,
        round.MissionResults.UnitResults.Sum(result => result.XpGained));
}
