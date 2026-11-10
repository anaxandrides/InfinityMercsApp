using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>A league as a member sees it: members by their league ids, never their accounts; the invite code only for the organizer.</summary>
public static class LeagueViews
{
    /// <summary>The league, showing one league season's entries and games.</summary>
    public static LeagueView For(LeagueDocument league, LeagueSeasonRecords season, string userId)
    {
        var you = league.MemberFor(userId);
        var organizer = league.IsOrganizer(userId);
        return new LeagueView
        {
            LeagueId = league.LeagueId,
            Name = league.Name,
            ArmySnapshotId = league.ArmySnapshotId,
            CurrentSeason = league.CurrentSeason,
            Season = season.Season,
            IsOrganizer = organizer,
            InviteCode = organizer ? league.InviteCode : null,
            Members = league.Members.Select(member => new LeagueMemberView
            {
                MemberId = member.MemberId,
                DisplayName = member.DisplayName,
                IsOrganizer = league.IsOrganizer(member.UserId),
                IsYou = member == you
            }).ToList(),
            Entries = season.Entries
                .OrderBy(entry => entry.CompanyName, StringComparer.CurrentCultureIgnoreCase)
                .Select(entry => new LeagueEntryView
                {
                    SeasonId = entry.SeasonId,
                    MemberId = entry.MemberId,
                    CompanyName = entry.CompanyName,
                    LeagueSeason = entry.LeagueSeason,
                    Renown = entry.Renown,
                    UpdatedAt = entry.UpdatedAt,
                    IsYours = entry.MemberId == you?.MemberId
                }).ToList(),
            Games = season.Games
                .OrderBy(game => game.RecordedAt)
                .Select(game => new LeagueGameView
                {
                    GameId = game.GameId,
                    LeagueSeason = game.LeagueSeason,
                    PlayedOn = game.PlayedOn,
                    Mission = game.Mission,
                    RecordedBy = game.RecordedBy,
                    Sides = game.Sides.Select(side => new LeagueGameSide { SeasonId = side.SeasonId, Result = side.Result, ObjectivePoints = side.ObjectivePoints }).ToList(),
                    CanRemove = organizer || game.RecordedBy == you?.MemberId
                }).ToList(),
            EnteredSeasonIds = [.. season.EnteredSeasonIds]
        };
    }

    public static LeagueSummary SummaryFor(LeagueDocument league, string userId) => new()
    {
        LeagueId = league.LeagueId,
        Name = league.Name,
        CurrentSeason = league.CurrentSeason,
        MemberCount = league.Members.Count,
        IsOrganizer = league.IsOrganizer(userId)
    };

    public static LeagueInvitePreview PreviewFor(LeagueDocument league, string userId) => new()
    {
        LeagueId = league.LeagueId,
        Name = league.Name,
        OrganizerName = league.Members.FirstOrDefault(member => league.IsOrganizer(member.UserId))?.DisplayName ?? string.Empty,
        MemberCount = league.Members.Count,
        IsMember = league.MemberFor(userId) is not null
    };
}
