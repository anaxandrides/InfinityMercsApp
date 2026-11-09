using InfinityMercsApp.Api.Leagues;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

public sealed class LeagueViewsTests
{
    [Fact]
    public void For_Organizer_ShowsTheInviteCode() => Assert.Equal("ABC234", LeagueViews.For(Sample(), Organizer).InviteCode);

    [Fact]
    public void For_Member_HidesTheInviteCode() => Assert.Null(LeagueViews.For(Sample(), Member).InviteCode);

    [Fact]
    public void For_Member_MarksWhatIsTheirs()
    {
        var view = LeagueViews.For(Sample() with { Games = [Recorded("g1", "m-m"), Recorded("g2", "m-o")] }, Member);

        Assert.Equal("m-m", Assert.Single(view.Members, member => member.IsYou).MemberId);
        Assert.Equal(MemberSeason, Assert.Single(view.Entries, entry => entry.IsYours).SeasonId);
        Assert.Equal(["g1"], view.Games.Where(game => game.CanRemove).Select(game => game.GameId));
        Assert.Equal("m-o", Assert.Single(view.Members, member => member.IsOrganizer).MemberId);
    }
}
