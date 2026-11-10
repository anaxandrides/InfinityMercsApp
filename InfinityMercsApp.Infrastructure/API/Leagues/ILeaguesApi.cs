using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Infrastructure.API.Leagues;

/// <summary>
/// Leagues online. Every call takes the access token, and never throws for a failed request: the reply says how it
/// went. A league comes with one league season's entries and games; calls that change it answer with the league as it
/// now is, showing the league season the change touched.
/// </summary>
public interface ILeaguesApi
{
    Task<LeaguesReply<List<LeagueSummary>>> ListAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<LeaguesReply<LeagueView>> CreateAsync(NewLeague league, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>The league, showing <paramref name="season"/>; the current league season when null.</summary>
    Task<LeaguesReply<LeagueView>> GetAsync(string leagueId, int? season, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>The organizer gives the league a new invite code; the old one stops working.</summary>
    Task<LeaguesReply<LeagueView>> NewInviteAsync(string leagueId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>The organizer closes joining.</summary>
    Task<LeaguesReply<LeagueView>> CloseInviteAsync(string leagueId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>What an invite code leads to.</summary>
    Task<LeaguesReply<LeagueInvitePreview>> PreviewInviteAsync(string code, string accessToken, CancellationToken cancellationToken = default);

    Task<LeaguesReply<LeagueView>> JoinAsync(string code, JoinLeague join, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>Enters one of the user's seasons, or brings its renown up to date.</summary>
    Task<LeaguesReply<LeagueView>> EnterAsync(string leagueId, string seasonId, LeagueEntryUpdate update, string accessToken, CancellationToken cancellationToken = default);

    Task<LeaguesReply<LeagueView>> WithdrawAsync(string leagueId, string seasonId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>A company entered in the league, with its season, for looking at.</summary>
    Task<LeaguesReply<LeagueEntryFiles>> EntryFilesAsync(string leagueId, string seasonId, string accessToken, CancellationToken cancellationToken = default);

    Task<LeaguesReply<LeagueView>> RecordGameAsync(string leagueId, NewLeagueGame game, string accessToken, CancellationToken cancellationToken = default);

    Task<LeaguesReply<LeagueView>> RemoveGameAsync(string leagueId, string gameId, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>The organizer moves the league to other army data.</summary>
    Task<LeaguesReply<LeagueView>> SetArmyAsync(string leagueId, LeagueArmyUpdate update, string accessToken, CancellationToken cancellationToken = default);

    /// <summary>The organizer starts the season after <paramref name="fromSeason"/>.</summary>
    Task<LeaguesReply<LeagueView>> StartSeasonAsync(string leagueId, int fromSeason, string accessToken, CancellationToken cancellationToken = default);
}
