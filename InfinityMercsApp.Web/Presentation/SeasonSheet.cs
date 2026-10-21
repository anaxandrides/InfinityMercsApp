using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Web.Presentation;

/// <summary>A season's page: its company, each contract in order, and the troopers who retired.</summary>
public sealed record SeasonSheet(
    string Id,
    string CompanyName,
    string CompanyId,
    string Started,
    IReadOnlyList<ContractRow> Contracts,
    IReadOnlyList<string> Retired)
{
    public int Wins => Contracts.Count(contract => contract.Result == "Won");

    public static SeasonSheet From(string id, SeasonFile season) => new(
        id,
        season.CompanyName,
        season.CompanyIdentifier,
        season.CreatedDate,
        season.Rounds.OrderBy(round => round.RoundIndex).Select(ContractRow.From).ToList(),
        season.Retired.Select(trooper => trooper.Name).ToList());
}
