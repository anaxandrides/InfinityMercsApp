using InfinityMercsApp.Api.Leagues.Changes;
using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>
/// Leagues: who belongs to them, the company seasons entered and the games recorded, shown a league season at a time.
/// A user who doesn't belong to a league can't tell it exists, except through its invite code.
/// </summary>
public sealed class LeagueService(ILeagueStore store, IUserDocumentStore files, IInviteCodeSource codes, TimeProvider time)
{
    /// <summary>How often a change is worked out again when the league keeps changing under it.</summary>
    public const int Attempts = 3;

    /// <summary>How many codes are tried before giving up on finding one not already issued.</summary>
    public const int CodeAttempts = 5;

    /// <summary>The leagues the user belongs to, by name.</summary>
    public async Task<IReadOnlyList<LeagueSummary>> ListAsync(string userId, CancellationToken cancellationToken = default) =>
        (await store.ListForMemberAsync(userId, cancellationToken))
            .Select(league => LeagueViews.SummaryFor(league, userId))
            .OrderBy(summary => summary.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>The league, or null when there is none or the user doesn't belong to it.</summary>
    public async Task<LeagueDocument?> GetAsync(string leagueId, string userId, CancellationToken cancellationToken = default) =>
        await store.ReadAsync(leagueId, cancellationToken) is { Document: var league } && league.MemberFor(userId) is not null
            ? league
            : null;

    /// <summary>
    /// The league as the user sees it, showing <paramref name="season"/> (the current one when null); null when there
    /// is no such league or league season, or the user doesn't belong to it.
    /// </summary>
    public async Task<LeagueView?> ViewAsync(string leagueId, string userId, int? season, CancellationToken cancellationToken = default)
    {
        if (await GetAsync(leagueId, userId, cancellationToken) is not { } league)
        {
            return null;
        }

        var shown = season ?? league.CurrentSeason;
        return shown >= 1 && shown <= league.CurrentSeason ? await ViewOfAsync(league, userId, shown, cancellationToken) : null;
    }

    /// <summary>Starts a league, organized by the user, open to join with a new invite code.</summary>
    public async Task<LeagueOutcome> CreateAsync(string userId, NewLeague request, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var leagueId = NewId();
        if (await IssueCodeAsync(leagueId, cancellationToken) is not { } code)
        {
            return LeagueOutcome.Conflict;
        }

        var league = new LeagueDocument(
            leagueId,
            leagueId,
            request.Name.Trim(),
            userId,
            request.ArmySnapshotId,
            CurrentSeason: 1,
            code,
            [],
            [],
            now)
            .WithMembers([new LeagueMember(NewId(), userId, request.DisplayName.Trim(), now)]);

        if (await store.TryCreateAsync(league, cancellationToken))
        {
            return LeagueOutcome.Done(LeagueViews.For(league, LeagueSeasonRecords.Empty(league.CurrentSeason), userId));
        }

        await store.DeleteInviteAsync(code, cancellationToken);
        return LeagueOutcome.Conflict;
    }

    /// <summary>
    /// Makes <paramref name="change"/> as the user, deciding it again if someone else wrote what it read first. The
    /// outcome shows the league season the change touched.
    /// </summary>
    public async Task<LeagueOutcome> ApplyAsync(string leagueId, string userId, ILeagueChange change, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (await store.ReadAsync(leagueId, cancellationToken) is not { } stored)
            {
                return LeagueOutcome.NotFound;
            }

            var decision = await change.DecideAsync(new LeagueReads(store, stored), userId, cancellationToken);
            if (decision.Status != LeagueChangeStatus.Done)
            {
                return LeagueOutcome.Refused(decision);
            }

            if (decision.Writes.IsEmpty || await store.TryCommitAsync(leagueId, decision.Writes, cancellationToken))
            {
                var league = decision.Writes.League?.Document ?? stored.Document;
                return LeagueOutcome.Done(await ViewOfAsync(league, userId, decision.Season, cancellationToken));
            }
        }

        return LeagueOutcome.Conflict;
    }

    /// <summary>The league an invite code leads to, or null when no league has the code.</summary>
    public async Task<LeagueInvitePreview?> PreviewInviteAsync(string code, string userId, CancellationToken cancellationToken = default) =>
        await LeagueForCodeAsync(code, cancellationToken) is { } league ? LeagueViews.PreviewFor(league, userId) : null;

    /// <summary>Joins the league the invite code leads to; a code since replaced or closed leads nowhere.</summary>
    public async Task<LeagueOutcome> JoinAsync(string code, string userId, JoinLeague request, CancellationToken cancellationToken = default)
    {
        if (await LeagueForCodeAsync(code, cancellationToken) is not { } league)
        {
            return LeagueOutcome.NotFound;
        }

        return await ApplyAsync(league.LeagueId, userId, new InviteJoinChange(code, new JoinChange(request.DisplayName, NewId(), time.GetUtcNow())), cancellationToken);
    }

    /// <summary>Gives the league a new invite code, which the organizer alone may do; the old code stops working.</summary>
    public async Task<LeagueOutcome> NewInviteAsync(string leagueId, string userId, CancellationToken cancellationToken = default)
    {
        if (await GetAsync(leagueId, userId, cancellationToken) is not { } league)
        {
            return LeagueOutcome.NotFound;
        }

        if (!league.IsOrganizer(userId))
        {
            return LeagueOutcome.Forbidden;
        }

        if (await IssueCodeAsync(leagueId, cancellationToken) is not { } code)
        {
            return LeagueOutcome.Conflict;
        }

        var result = await ApplyAsync(leagueId, userId, new InviteCodeChange(code), cancellationToken);
        if (result.Status != LeagueChangeStatus.Done)
        {
            await store.DeleteInviteAsync(code, cancellationToken);
        }
        else if (league.InviteCode is { } replaced)
        {
            await store.DeleteInviteAsync(replaced, cancellationToken);
        }

        return result;
    }

    /// <summary>Closes joining, which the organizer alone may do; the code stops working.</summary>
    public async Task<LeagueOutcome> CloseInviteAsync(string leagueId, string userId, CancellationToken cancellationToken = default)
    {
        var league = await GetAsync(leagueId, userId, cancellationToken);
        var result = await ApplyAsync(leagueId, userId, new InviteCodeChange(null), cancellationToken);
        if (result.Status == LeagueChangeStatus.Done && league?.InviteCode is { } code)
        {
            await store.DeleteInviteAsync(code, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Enters one of the user's seasons in the league, or brings its renown up to date. The season must be among the
    /// user's files online; its company's name comes from there.
    /// </summary>
    public async Task<LeagueOutcome> EnterAsync(string leagueId, string userId, string seasonId, LeagueEntryUpdate update, CancellationToken cancellationToken = default)
    {
        if (await files.ReadAsync<SeasonFile>(userId, UserDocumentKind.Season, seasonId, cancellationToken) is not { Document: { Deleted: false, Content: { } season } })
        {
            return await GetAsync(leagueId, userId, cancellationToken) is null
                ? LeagueOutcome.NotFound
                : LeagueOutcome.Rejected("That season isn't among your files online yet; let it sync first.");
        }

        return await ApplyAsync(leagueId, userId, new EnterSeasonChange(seasonId, season.CompanyName, update.Renown, time.GetUtcNow()), cancellationToken);
    }

    /// <summary>
    /// A company entered in the league, with its season, as its owner keeps them now; null when the user doesn't
    /// belong to the league or nothing by that id is entered.
    /// </summary>
    public async Task<LeagueEntryFiles?> EntryFilesAsync(string leagueId, string userId, string seasonId, CancellationToken cancellationToken = default)
    {
        if (await GetAsync(leagueId, userId, cancellationToken) is not { } league ||
            await store.ReadEntryAsync(leagueId, seasonId, cancellationToken) is not { Document: var entry } ||
            league.Members.FirstOrDefault(member => member.MemberId == entry.MemberId) is not { } owner)
        {
            return null;
        }

        var season = (await files.ReadAsync<SeasonFile>(owner.UserId, UserDocumentKind.Season, seasonId, cancellationToken))?.Document;
        var seasonFile = season is { Deleted: false } ? season.Content : null;
        var company = seasonFile is null
            ? null
            : (await files.ReadAsync<SavedCompanyFile>(owner.UserId, UserDocumentKind.Company, seasonFile.CompanyIdentifier, cancellationToken))?.Document;
        return new LeagueEntryFiles { Season = seasonFile, Company = company is { Deleted: false } ? company.Content : null };
    }

    private async Task<LeagueDocument?> LeagueForCodeAsync(string code, CancellationToken cancellationToken) =>
        await store.FindInviteAsync(code, cancellationToken) is { } leagueId &&
        await store.ReadAsync(leagueId, cancellationToken) is { Document: { InviteCode: { } current } league } &&
        current == code
            ? league
            : null;

    private async Task<LeagueView> ViewOfAsync(LeagueDocument league, string userId, int season, CancellationToken cancellationToken)
    {
        var entries = store.ListEntriesAsync(league.LeagueId, season, cancellationToken);
        var games = store.ListGamesAsync(league.LeagueId, season, cancellationToken);
        var entered = league.MemberFor(userId) is { } member
            ? store.ListEnteredSeasonIdsAsync(league.LeagueId, member.MemberId, cancellationToken)
            : Task.FromResult<IReadOnlyList<string>>([]);
        await Task.WhenAll(entries, games, entered);
        return LeagueViews.For(league, new LeagueSeasonRecords(season, await entries, await games, await entered), userId);
    }

    private async Task<string?> IssueCodeAsync(string leagueId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < CodeAttempts; attempt++)
        {
            var code = codes.Next();
            if (await store.TryCreateInviteAsync(LeagueInvite.For(code, leagueId), cancellationToken))
            {
                return code;
            }
        }

        return null;
    }

    private static string NewId() => Guid.NewGuid().ToString("N");
}
