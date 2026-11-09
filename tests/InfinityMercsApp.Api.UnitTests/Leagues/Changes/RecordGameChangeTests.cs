using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class RecordGameChangeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Apply_PlayerInTheGame_RecordsItForTheCurrentSeason()
    {
        var result = new RecordGameChange(Game(), "g1", Now).Apply(Sample(), Member);

        var game = Assert.Single(result.League?.Games ?? []);
        Assert.Equal("m-m", game.RecordedBy);
        Assert.Equal(1, game.LeagueSeason);
        Assert.Equal(7, game.Sides[0].ObjectivePoints);
    }

    [Fact]
    public void Apply_MemberNotInTheGame_IsRejected()
    {
        var league = Sample();
        league = new JoinChange("Nova", "m-n", Now).Apply(league, Outsider).League ?? league;

        Assert.Equal(LeagueChangeStatus.Rejected, new RecordGameChange(Game(), "g1", Now).Apply(league, Outsider).Status);
    }

    [Fact]
    public void Apply_CompanyNotEntered_IsRejected() =>
        Assert.Equal(LeagueChangeStatus.Rejected, new RecordGameChange(Game(loser: "season-x"), "g1", Now).Apply(Sample(), Member).Status);

    [Fact]
    public void Apply_CompanyFromAnEarlierSeason_IsRejected()
    {
        var league = Sample() with { CurrentSeason = 2 };

        Assert.Equal(LeagueChangeStatus.Rejected, new RecordGameChange(Game(), "g1", Now).Apply(league, Member).Status);
    }

    [Fact]
    public void Apply_NotAMember_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, new RecordGameChange(Game(), "g1", Now).Apply(Sample(), Outsider).Status);
}
