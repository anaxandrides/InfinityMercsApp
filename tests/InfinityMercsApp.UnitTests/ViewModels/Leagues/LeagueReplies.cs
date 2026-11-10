using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;

namespace InfinityMercsApp.UnitTests.ViewModels.Leagues;

/// <summary>Leagues as the API answers with them.</summary>
internal static class LeagueReplies
{
    public static LeaguesReply<T> Ok<T>(T value)
        where T : class => new(LeaguesStatus.Ok, value, []);

    public static LeaguesReply<T> Failed<T>(LeaguesStatus status, params IEnumerable<string> problems)
        where T : class => new(status, null, problems.ToList());

    /// <summary>
    /// A league whose first season has the user's "Red Company" against Mo's "Blue Company", with one game played;
    /// later seasons are empty. It shows <paramref name="season"/>, or the current season when null, as the API does.
    /// </summary>
    public static LeagueView Sample(bool organizer = true, int currentSeason = 1, int ownRenown = 300, string? inviteCode = "ABC234", int? season = null) =>
        (season ?? currentSeason) == 1 ? FirstSeason(organizer, currentSeason, ownRenown, inviteCode) : LaterSeason(organizer, currentSeason, ownRenown, inviteCode, season ?? currentSeason);

    private static LeagueView LaterSeason(bool organizer, int currentSeason, int ownRenown, string? inviteCode, int season)
    {
        var league = FirstSeason(organizer, currentSeason, ownRenown, inviteCode);
        league.Season = season;
        league.Entries = [];
        league.Games = [];
        return league;
    }

    private static LeagueView FirstSeason(bool organizer, int currentSeason, int ownRenown, string? inviteCode) => new()
    {
        LeagueId = "l1",
        Name = "Spring League",
        CurrentSeason = currentSeason,
        Season = 1,
        EnteredSeasonIds = ["season-red"],
        IsOrganizer = organizer,
        InviteCode = organizer ? inviteCode : null,
        Members =
        [
            new LeagueMemberView { MemberId = "m-you", DisplayName = "Olive", IsOrganizer = organizer, IsYou = true },
            new LeagueMemberView { MemberId = "m-mo", DisplayName = "Mo", IsOrganizer = !organizer }
        ],
        Entries =
        [
            new LeagueEntryView { SeasonId = "season-red", MemberId = "m-you", CompanyName = "Red Company", LeagueSeason = 1, Renown = ownRenown, IsYours = true },
            new LeagueEntryView { SeasonId = "season-blue", MemberId = "m-mo", CompanyName = "Blue Company", LeagueSeason = 1, Renown = 350 }
        ],
        Games =
        [
            new LeagueGameView
            {
                GameId = "g1",
                LeagueSeason = 1,
                PlayedOn = new DateOnly(2026, 11, 1),
                Mission = "Supplies",
                RecordedBy = "m-mo",
                Sides =
                [
                    new LeagueGameSide { SeasonId = "season-red", Result = GameResult.Win, ObjectivePoints = 7 },
                    new LeagueGameSide { SeasonId = "season-blue", Result = GameResult.Loss, ObjectivePoints = 3 }
                ]
            }
        ]
    };
}
