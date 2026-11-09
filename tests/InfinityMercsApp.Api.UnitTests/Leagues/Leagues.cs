using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

/// <summary>A league organized by "organizer", with "member" in it, each with a company season entered.</summary>
internal static class Leagues
{
    public const string Organizer = "organizer";
    public const string Member = "member";
    public const string Outsider = "outsider";
    public const string OrganizerSeason = "season-o";
    public const string MemberSeason = "season-m";

    public static LeagueDocument Sample(string? inviteCode = "ABC234") =>
        new LeagueDocument(
            "league-1",
            "league-1",
            "Spring League",
            Organizer,
            "snap-1",
            CurrentSeason: 1,
            inviteCode,
            [],
            [],
            [
                new LeagueEntry(OrganizerSeason, "m-o", "Red Company", 1, 300, DateTimeOffset.UnixEpoch),
                new LeagueEntry(MemberSeason, "m-m", "Blue Company", 1, 250, DateTimeOffset.UnixEpoch)
            ],
            [],
            DateTimeOffset.UnixEpoch)
        .WithMembers(
        [
            new LeagueMember("m-o", Organizer, "Olive", DateTimeOffset.UnixEpoch),
            new LeagueMember("m-m", Member, "Mo", DateTimeOffset.UnixEpoch)
        ]);

    public static NewLeagueGame Game(string winner = OrganizerSeason, string loser = MemberSeason) => new()
    {
        PlayedOn = new DateOnly(2026, 11, 1),
        Mission = "Supplies",
        Sides =
        [
            new LeagueGameSide { SeasonId = winner, Result = GameResult.Win, ObjectivePoints = 7 },
            new LeagueGameSide { SeasonId = loser, Result = GameResult.Loss, ObjectivePoints = 3 }
        ]
    };

    public static LeagueGame Recorded(string gameId, string recordedBy) =>
        new(gameId, 1, new DateOnly(2026, 11, 1), "Supplies", recordedBy, DateTimeOffset.UnixEpoch, Game().Sides);
}
