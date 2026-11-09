using InfinityMercsApp.Api.Leagues.Changes;
using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>
/// Leagues: who belongs to them, the company seasons entered and the games recorded. A user who doesn't belong to a
/// league can't tell it exists, except through its invite code.
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
        await store.ReadAsync(leagueId, cancellationToken) is { } stored && stored.League.MemberFor(userId) is not null
            ? stored.League
            : null;

    /// <summary>Starts a league, organized by the user, open to join with a new invite code.</summary>
    public async Task<LeagueChangeResult> CreateAsync(string userId, NewLeague request, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var leagueId = NewId();
        if (await IssueCodeAsync(leagueId, cancellationToken) is not { } code)
        {
            return LeagueChangeResult.Conflict;
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
            [],
            [],
            now)
            .WithMembers([new LeagueMember(NewId(), userId, request.DisplayName.Trim(), now)]);

        if (await store.TryCreateAsync(league, cancellationToken))
        {
            return LeagueChangeResult.Done(league);
        }

        await store.DeleteInviteAsync(code, cancellationToken);
        return LeagueChangeResult.Conflict;
    }

    /// <summary>Makes <paramref name="change"/> as the user, working it out again if someone else changed the league first.</summary>
    public async Task<LeagueChangeResult> ApplyAsync(string leagueId, string userId, ILeagueChange change, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (await store.ReadAsync(leagueId, cancellationToken) is not { } stored)
            {
                return LeagueChangeResult.NotFound;
            }

            var result = change.Apply(stored.League, userId);
            if (result is not { Status: LeagueChangeStatus.Done, League: { } changed } || changed == stored.League)
            {
                return result;
            }

            if (await store.TryReplaceAsync(changed, stored.ETag, cancellationToken))
            {
                return result;
            }
        }

        return LeagueChangeResult.Conflict;
    }

    /// <summary>The league an invite code leads to, or null when no league has the code.</summary>
    public async Task<LeagueInvitePreview?> PreviewInviteAsync(string code, string userId, CancellationToken cancellationToken = default) =>
        await LeagueForCodeAsync(code, cancellationToken) is { } league ? LeagueViews.PreviewFor(league, userId) : null;

    /// <summary>Joins the league the invite code leads to; a code since replaced or closed leads nowhere.</summary>
    public async Task<LeagueChangeResult> JoinAsync(string code, string userId, JoinLeague request, CancellationToken cancellationToken = default)
    {
        if (await LeagueForCodeAsync(code, cancellationToken) is not { } league)
        {
            return LeagueChangeResult.NotFound;
        }

        return await ApplyAsync(league.LeagueId, userId, new InviteJoinChange(code, new JoinChange(request.DisplayName, NewId(), time.GetUtcNow())), cancellationToken);
    }

    /// <summary>Gives the league a new invite code, which the organizer alone may do; the old code stops working.</summary>
    public async Task<LeagueChangeResult> NewInviteAsync(string leagueId, string userId, CancellationToken cancellationToken = default)
    {
        if (await GetAsync(leagueId, userId, cancellationToken) is not { } league)
        {
            return LeagueChangeResult.NotFound;
        }

        if (!league.IsOrganizer(userId))
        {
            return LeagueChangeResult.Forbidden;
        }

        if (await IssueCodeAsync(leagueId, cancellationToken) is not { } code)
        {
            return LeagueChangeResult.Conflict;
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
    public async Task<LeagueChangeResult> CloseInviteAsync(string leagueId, string userId, CancellationToken cancellationToken = default)
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
    public async Task<LeagueChangeResult> EnterAsync(string leagueId, string userId, string seasonId, LeagueEntryUpdate update, CancellationToken cancellationToken = default)
    {
        if (await files.ReadAsync<SeasonFile>(userId, UserDocumentKind.Season, seasonId, cancellationToken) is not { Document: { Deleted: false, Content: { } season } })
        {
            return await GetAsync(leagueId, userId, cancellationToken) is null
                ? LeagueChangeResult.NotFound
                : LeagueChangeResult.Rejected("That season isn't among your files online yet; let it sync first.");
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
            league.Entries.FirstOrDefault(entry => entry.SeasonId == seasonId) is not { } entry ||
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
        await store.ReadAsync(leagueId, cancellationToken) is { League.InviteCode: { } current } stored &&
        current == code
            ? stored.League
            : null;

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
