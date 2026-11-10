using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// A member records a game their company played against other companies in the league's current season. One player
/// records it for everyone. Each company's entry counts the game, so it can't be withdrawn while the game stands.
/// </summary>
public sealed class RecordGameChange(NewLeagueGame game, string gameId, DateTimeOffset now) : ILeagueChange
{
    /// <inheritdoc/>
    public async Task<LeagueDecision> DecideAsync(LeagueReads reads, string userId, CancellationToken cancellationToken = default)
    {
        var league = reads.League;
        if (league.MemberFor(userId) is not { } member)
        {
            return LeagueDecision.NotFound;
        }

        var entries = new List<StoredDocument<LeagueEntry>>();
        foreach (var side in game.Sides)
        {
            if (await reads.EntryAsync(side.SeasonId, cancellationToken) is not { } stored)
            {
                return LeagueDecision.Rejected("A company in the game isn't in the league.");
            }

            if (stored.Document.LeagueSeason != league.CurrentSeason)
            {
                return LeagueDecision.Rejected($"{stored.Document.CompanyName} was in an earlier league season.");
            }

            entries.Add(stored);
        }

        if (entries.All(stored => stored.Document.MemberId != member.MemberId))
        {
            return LeagueDecision.Rejected("Only a player whose company played can record the game.");
        }

        var sides = game.Sides
            .Select(side => new LeagueGameSide { SeasonId = side.SeasonId, Result = side.Result, ObjectivePoints = side.ObjectivePoints })
            .ToList();
        var recorded = new LeagueGame(
            LeagueGame.IdFor(gameId), league.LeagueId, gameId, league.CurrentSeason, game.PlayedOn, game.Mission.Trim(), member.MemberId, now, sides);
        var writes = new LeagueWrites(
            null,
            entries.Select(stored => DocumentWrites.Replace(stored.Document with { GamesPlayed = stored.Document.GamesPlayed + 1 }, stored.ETag)).ToList(),
            [DocumentWrites.Create(recorded)]);
        return LeagueDecision.Done(writes, league.CurrentSeason);
    }
}
