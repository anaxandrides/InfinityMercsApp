using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class EnterSeasonChangeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Apply_NewSeason_EntersItInTheCurrentLeagueSeason()
    {
        var league = Sample() with { CurrentSeason = 2 };

        var result = new EnterSeasonChange("season-new", "Green Company", 210, Now).Apply(league, Member);

        var entry = Assert.Single(result.League?.Entries ?? [], e => e.SeasonId == "season-new");
        Assert.Equal("m-m", entry.MemberId);
        Assert.Equal(2, entry.LeagueSeason);
        Assert.Equal(210, entry.Renown);
    }

    [Fact]
    public void Apply_OwnSeasonEntered_UpdatesItsRenownAndName()
    {
        var result = new EnterSeasonChange(MemberSeason, "Blue Company II", 280, Now).Apply(Sample(), Member);

        var entry = Assert.Single(result.League?.Entries ?? [], e => e.SeasonId == MemberSeason);
        Assert.Equal(280, entry.Renown);
        Assert.Equal("Blue Company II", entry.CompanyName);
        Assert.Equal(Now, entry.UpdatedAt);
        Assert.Equal(2, result.League?.Entries.Count);
    }

    [Fact]
    public void Apply_SomeoneElsesSeason_IsForbidden() =>
        Assert.Equal(LeagueChangeStatus.Forbidden, new EnterSeasonChange(OrganizerSeason, "Mine now", 1, Now).Apply(Sample(), Member).Status);

    [Fact]
    public void Apply_NotAMember_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, new EnterSeasonChange("season-x", "X", 1, Now).Apply(Sample(), Outsider).Status);
}
