using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class WithdrawChangeTests
{
    [Fact]
    public void Apply_OwnSeasonNotPlayed_TakesItOut()
    {
        var result = new WithdrawChange(MemberSeason).Apply(Sample(), Member);

        Assert.DoesNotContain(result.League?.Entries ?? [], entry => entry.SeasonId == MemberSeason);
    }

    [Fact]
    public void Apply_OrganizerTakingOutAnyone_TakesItOut() =>
        Assert.Equal(LeagueChangeStatus.Done, new WithdrawChange(MemberSeason).Apply(Sample(), Organizer).Status);

    [Fact]
    public void Apply_SomeoneElsesSeason_IsForbidden() =>
        Assert.Equal(LeagueChangeStatus.Forbidden, new WithdrawChange(OrganizerSeason).Apply(Sample(), Member).Status);

    [Fact]
    public void Apply_SeasonHasPlayed_IsRejected()
    {
        var league = Sample() with { Games = [Recorded("g1", "m-o")] };

        Assert.Equal(LeagueChangeStatus.Rejected, new WithdrawChange(MemberSeason).Apply(league, Member).Status);
    }

    [Fact]
    public void Apply_NotEntered_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, new WithdrawChange("season-x").Apply(Sample(), Member).Status);
}
