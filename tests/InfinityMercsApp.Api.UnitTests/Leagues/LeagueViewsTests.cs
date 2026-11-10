using InfinityMercsApp.Api.Leagues;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

public sealed class LeagueViewsTests
{
    private static LeagueSeasonRecords Season(params IEnumerable<LeagueGame> games) =>
        new(1, [MemberEntry, OrganizerEntry], [.. games], [MemberSeason]);

    [Fact]
    public void For_Organizer_ShowsTheInviteCode() => Assert.Equal("ABC234", LeagueViews.For(Sample(), Season(), Organizer).InviteCode);

    [Fact]
    public void For_Member_HidesTheInviteCode() => Assert.Null(LeagueViews.For(Sample(), Season(), Member).InviteCode);

    [Fact]
    public void For_Member_MarksWhatIsTheirs()
    {
        var view = LeagueViews.For(Sample(), Season(Recorded("g1", "m-m"), Recorded("g2", "m-o")), Member);

        Assert.Equal("m-m", Assert.Single(view.Members, member => member.IsYou).MemberId);
        Assert.Equal(MemberSeason, Assert.Single(view.Entries, entry => entry.IsYours).SeasonId);
        Assert.Equal(["g1"], view.Games.Where(game => game.CanRemove).Select(game => game.GameId));
        Assert.Equal("m-o", Assert.Single(view.Members, member => member.IsOrganizer).MemberId);
    }

    [Fact]
    public void For_AnySeason_ShowsWhichSeasonAndWhatTheUserEntered()
    {
        var view = LeagueViews.For(Sample() with { CurrentSeason = 2 }, Season(), Member);

        Assert.Equal(1, view.Season);
        Assert.Equal(2, view.CurrentSeason);
        Assert.Equal([MemberSeason], view.EnteredSeasonIds);
    }

    [Fact]
    public void For_Entries_ListsThemByCompanyName() =>
        Assert.Equal(["Blue Company", "Red Company"], LeagueViews.For(Sample(), Season(), Member).Entries.Select(entry => entry.CompanyName));
}
