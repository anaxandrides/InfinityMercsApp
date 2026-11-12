using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

/// <summary>A league organized by "organizer", with "member" in it, each with a company season entered.</summary>
internal static class Leagues
{
    public const string LeagueId = "league-1";
    public const string Organizer = "organizer";
    public const string Member = "member";
    public const string Outsider = "outsider";
    public const string OrganizerSeason = "season-o";
    public const string MemberSeason = "season-m";

    public static LeagueDocument Sample(string? inviteCode = "ABC234") =>
        new LeagueDocument(
            LeagueId,
            LeagueId,
            "Spring League",
            Organizer,
            "snap-1",
            CurrentSeason: 1,
            inviteCode,
            [],
            [],
            DateTimeOffset.UnixEpoch)
        .WithMembers(
        [
            new LeagueMember("m-o", Organizer, "Olive", DateTimeOffset.UnixEpoch),
            new LeagueMember("m-m", Member, "Mo", DateTimeOffset.UnixEpoch)
        ]);

    public static LeagueEntry OrganizerEntry { get; } = LeagueEntry.For(LeagueId, OrganizerSeason, "m-o", "Red Company", 1, 300, DateTimeOffset.UnixEpoch);

    public static LeagueEntry MemberEntry { get; } = LeagueEntry.For(LeagueId, MemberSeason, "m-m", "Blue Company", 1, 250, DateTimeOffset.UnixEpoch);

    /// <summary>A store holding the sample league, its two entries and its invite code.</summary>
    public static InMemoryLeagueStore Store(LeagueDocument? league = null)
    {
        var store = new InMemoryLeagueStore();
        store.Keep(league ?? Sample());
        store.Keep(OrganizerEntry);
        store.Keep(MemberEntry);
        store.Invites["ABC234"] = LeagueId;
        return store;
    }

    /// <summary>The sample league as a change reads it from <paramref name="store"/>.</summary>
    public static async Task<LeagueReads> ReadsAsync(InMemoryLeagueStore store) =>
        new(store, await store.ReadAsync(LeagueId) ?? throw new InvalidOperationException("The sample league isn't kept."));

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

    public static LeagueGame Recorded(string gameId, string recordedBy, int leagueSeason = 1) =>
        new(LeagueGame.IdFor(gameId), LeagueId, gameId, leagueSeason, new DateOnly(2026, 11, 1), "Supplies", recordedBy, DateTimeOffset.UnixEpoch, Game().Sides);
}
