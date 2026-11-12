namespace InfinityMercsApp.Api.Leagues;

/// <summary>A document kept in a league's partition: the league itself, an entry or a game.</summary>
public interface ILeagueItem
{
    /// <summary>The document id, unique within the league's partition.</summary>
    string Id { get; }

    /// <summary>The league's id, which is the partition key.</summary>
    string LeagueId { get; }
}
