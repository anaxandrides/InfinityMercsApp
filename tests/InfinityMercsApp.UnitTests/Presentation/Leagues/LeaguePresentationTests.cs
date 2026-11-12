using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Presentation.Leagues;
using InfinityMercsApp.Services.SignIn;
using static InfinityMercsApp.UnitTests.ViewModels.Leagues.LeagueReplies;

namespace InfinityMercsApp.UnitTests.Presentation.Leagues;

public sealed class LeaguePresentationTests
{
    [Fact]
    public void LeagueStandings_For_RanksByRenownThenWinsThenObjectivePoints()
    {
        var league = Sample(ownRenown: 350);

        var standings = LeagueStandings.For(league, 1);

        Assert.Equal(["Red Company", "Blue Company"], standings.Select(line => line.CompanyName));
        Assert.Equal([1, 2], standings.Select(line => line.Place));
        Assert.Equal("Mo", standings[1].PlayerName);
        Assert.Equal(7, standings[0].ObjectivePoints);
        Assert.Equal("0-0-1", standings[1].Record);
    }

    [Fact]
    public void LeagueStandings_For_LeavesOutOtherSeasons() => Assert.Empty(LeagueStandings.For(Sample(), 2));

    [Fact]
    public void LeagueNames_LeftMemberAndWithdrawnCompany_AreNamedAsSuch()
    {
        var league = Sample();

        Assert.Equal(LeagueNames.FormerMember, LeagueNames.PlayerOf(league, "m-gone"));
        Assert.Equal(LeagueNames.WithdrawnCompany, LeagueNames.CompanyOf(league, "season-gone"));
    }

    [Fact]
    public void LeagueGameRow_Draw_SaysBothDrew()
    {
        var league = Sample();
        league.Games[0].Sides.ForEach(side => side.Result = GameResult.Draw);

        Assert.Equal("Red Company 7 (drew) · Blue Company 3 (drew)", LeagueGameRow.For(league.Games[0], league).Score);
    }

    [Fact]
    public void LeagueMemberRow_OrganizerWhoIsYou_SaysBoth() =>
        Assert.Equal("organizer, you", LeagueMemberRow.For(new LeagueMemberView { DisplayName = "Olive", IsOrganizer = true, IsYou = true }).Note);

    [Fact]
    public void LeagueEntryChoice_SomeoneElses_NamesTheirPlayer() =>
        Assert.Equal("Blue Company (Mo)", LeagueEntryChoice.For(Sample().Entries[1], Sample()).Text);

    [Theory]
    [InlineData("Olive", "Olive")]
    [InlineData("olive@example.com", "olive")]
    [InlineData("  ", DisplayNames.Fallback)]
    public void DisplayNames_Of_ShowsTheNameWithoutTheEmailDomain(string displayName, string expected) =>
        Assert.Equal(expected, DisplayNames.Of(new SignedInAccount("user", displayName)));

    [Fact]
    public void DisplayNames_Of_CutsLongNamesToFit() =>
        Assert.Equal(40, DisplayNames.Of(new SignedInAccount("user", new string('a', 60))).Length);

    [Fact]
    public void LeagueSeasonChoice_UpTo_ListsTheCurrentSeasonFirst() =>
        Assert.Equal(["Season 3 (now)", "Season 2", "Season 1"], LeagueSeasonChoice.UpTo(3).Select(season => season.Text));

    [Theory]
    [InlineData(LeaguesStatus.Forbidden, "organizer")]
    [InlineData(LeaguesStatus.Conflict, "Try again")]
    [InlineData(LeaguesStatus.Unauthorized, "Log in again")]
    [InlineData(LeaguesStatus.Rejected, "refused")]
    public void LeagueMessages_For_Status_SaysWhatHappened(LeaguesStatus status, string expected) =>
        Assert.Contains(expected, LeagueMessages.For(status, []));

    [Fact]
    public void InviteQr_For_Link_IsASquareWithAQuietZone()
    {
        var qr = InviteQr.For("https://www.infinitythemercs.com/join/ABC234");

        Assert.True(qr.Size >= 29);
        Assert.StartsWith("M4 4h1v1h-1z", qr.Path);
    }

    [Fact]
    public void ResultChoice_Opposite_IsHowItWentForTheOtherSide() =>
        Assert.Equal([GameResult.Loss, GameResult.Draw, GameResult.Win], ResultChoice.All.Select(choice => choice.Opposite));
}
