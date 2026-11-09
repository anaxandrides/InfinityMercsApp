using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class RemoveGameChangeTests
{
    [Theory]
    [InlineData(Member)]
    [InlineData(Organizer)]
    public void Apply_RecorderOrOrganizer_RemovesIt(string userId)
    {
        var league = Sample() with { Games = [Recorded("g1", "m-m")] };

        var result = new RemoveGameChange("g1").Apply(league, userId);

        Assert.Equal(LeagueChangeStatus.Done, result.Status);
        Assert.Empty(result.League?.Games ?? []);
    }

    [Fact]
    public void Apply_SomeoneElsesGame_IsForbidden()
    {
        var league = Sample() with { Games = [Recorded("g1", "m-o")] };

        Assert.Equal(LeagueChangeStatus.Forbidden, new RemoveGameChange("g1").Apply(league, Member).Status);
    }

    [Fact]
    public void Apply_NoSuchGame_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, new RemoveGameChange("g1").Apply(Sample(), Member).Status);
}
