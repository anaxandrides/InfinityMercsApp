using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

/// <summary>The changes only the organizer may make.</summary>
public sealed class OrganizerChangeTests
{
    [Fact]
    public void ArmyChange_Organizer_MovesTheLeague() =>
        Assert.Equal("snap-2", new ArmyChange("snap-2").Apply(Sample(), Organizer).League?.ArmySnapshotId);

    [Fact]
    public void ArmyChange_Member_IsForbidden() =>
        Assert.Equal(LeagueChangeStatus.Forbidden, new ArmyChange("snap-2").Apply(Sample(), Member).Status);

    [Fact]
    public void ArmyChange_NotAMember_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, new ArmyChange("snap-2").Apply(Sample(), Outsider).Status);

    [Fact]
    public void InviteCodeChange_NoCode_ClosesJoining()
    {
        var result = new InviteCodeChange(null).Apply(Sample(), Organizer);

        Assert.Equal(LeagueChangeStatus.Done, result.Status);
        Assert.Null(result.League?.InviteCode);
    }

    [Fact]
    public void NextSeasonChange_FromTheCurrentSeason_StartsTheNext() =>
        Assert.Equal(2, new NextSeasonChange(1).Apply(Sample(), Organizer).League?.CurrentSeason);

    [Fact]
    public void NextSeasonChange_FromAnEarlierSeason_LeavesTheLeagueAsItWas()
    {
        var league = Sample() with { CurrentSeason = 3 };

        Assert.Same(league, new NextSeasonChange(2).Apply(league, Organizer).League);
    }
}
