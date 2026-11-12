using Bunit;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Web.Pages.Leagues;
using Microsoft.AspNetCore.Components;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>The league pages over the shared view models, with the leagues API faked.</summary>
public sealed class LeaguePagesTests : SiteContext
{
    public LeaguePagesTests()
    {
        Authorization.SetAuthorized("Kestrel");
        Leagues.ListAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Reply(new List<LeagueSummary> { new() { LeagueId = "l1", Name = "Spring League", CurrentSeason = 1, MemberCount = 2, IsOrganizer = true } }));
        Leagues.GetAsync("l1", Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Reply(League()));
    }

    private static LeaguesReply<T> Reply<T>(T value)
        where T : class => new(LeaguesStatus.Ok, value, []);

    private static LeagueView League() => new()
    {
        LeagueId = "l1",
        Name = "Spring League",
        CurrentSeason = 1,
        Season = 1,
        IsOrganizer = true,
        InviteCode = "ABC234",
        Members =
        [
            new LeagueMemberView { MemberId = "m1", DisplayName = "Kestrel", IsOrganizer = true, IsYou = true },
            new LeagueMemberView { MemberId = "m2", DisplayName = "Mo" }
        ],
        Entries = [new LeagueEntryView { SeasonId = "s2", MemberId = "m2", CompanyName = "Blue Company", LeagueSeason = 1, Renown = 250 }],
        Games = []
    };

    [Fact]
    public void LeaguesPage_SignedIn_ListsTheirLeaguesAndTheForms()
    {
        var page = Render<LeaguesPage>();

        page.WaitForAssertion(() => Assert.Contains("Spring League", page.Find(".leagues").TextContent, StringComparison.Ordinal));
        Assert.Contains("you organize it", page.Markup, StringComparison.Ordinal);
        Assert.NotNull(page.Find("input.code"));
    }

    [Fact]
    public void LeaguesPage_StartLeague_CreatesItAndOpensIt()
    {
        Leagues.CreateAsync(Arg.Any<NewLeague>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Reply(League()));
        var page = Render<LeaguesPage>();
        page.WaitForElement("input[aria-label='League name']").Input("Spring League");

        page.FindAll("button.primary").Single(button => button.TextContent == "START").Click();

        page.WaitForAssertion(() => Assert.EndsWith("/leagues/l1", Get<NavigationManager>().Uri, StringComparison.Ordinal));
        Leagues.Received(1).CreateAsync(Arg.Is<NewLeague>(league => league.Name == "Spring League" && league.DisplayName == "Kestrel"), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void LeaguesPage_TypedCode_OpensTheInvitePage()
    {
        var page = Render<LeaguesPage>();
        page.WaitForElement("input.code").Input("abc-234");

        page.FindAll("button.primary").Single(button => button.TextContent == "JOIN").Click();

        page.WaitForAssertion(() => Assert.EndsWith("/join/ABC234", Get<NavigationManager>().Uri, StringComparison.Ordinal));
    }

    [Fact]
    public void JoinLeaguePage_GoodCode_JoinsAndOpensTheLeague()
    {
        Leagues.PreviewInviteAsync("ABC234", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Reply(new LeagueInvitePreview { LeagueId = "l1", Name = "Spring League", OrganizerName = "Olive", MemberCount = 3 }));
        Leagues.JoinAsync("ABC234", Arg.Any<JoinLeague>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Reply(League()));
        var page = Render<JoinLeaguePage>(parameters => parameters.Add(p => p.Code, "abc234"));

        page.WaitForAssertion(() => Assert.Contains("Run by Olive · 3 members", page.Markup, StringComparison.Ordinal));
        page.Find(".card button.primary").Click();

        page.WaitForAssertion(() => Assert.EndsWith("/leagues/l1", Get<NavigationManager>().Uri, StringComparison.Ordinal));
    }

    [Fact]
    public void LeaguePage_Organizer_ShowsStandingsMembersAndTheInviteQrCode()
    {
        var page = Render<LeaguePage>(parameters => parameters.Add(p => p.LeagueId, "l1"));

        page.WaitForAssertion(() => Assert.Contains("Blue Company", page.Find(".standings").TextContent, StringComparison.Ordinal), Patience);
        Assert.Equal("ABC234", page.Find(".invite-code").TextContent);
        Assert.Contains("https://www.infinitythemercs.com/join/ABC234", page.Find(".invite").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.Find("svg.invite-qr path").GetAttribute("d") ?? string.Empty);
        Assert.Contains("Mo", page.Find(".members").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void LeaguePage_CompanyName_OpensTheCompany()
    {
        var page = Render<LeaguePage>(parameters => parameters.Add(p => p.LeagueId, "l1"));
        page.WaitForAssertion(() => page.Find(".standings button.link").Click(), Patience);

        page.WaitForAssertion(() => Assert.EndsWith("/leagues/l1/companies/s2", Get<NavigationManager>().Uri, StringComparison.Ordinal));
    }

    [Fact]
    public void LeagueCompanyPage_Entered_ShowsItsTroopers()
    {
        Leagues.EntryFilesAsync("l1", "s2", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Reply(new LeagueEntryFiles
        {
            Company = new SavedCompanyFile
            {
                CompanyName = "Blue Company",
                CompanyIdentifier = "c2",
                Entries = [new SavedCompanyEntry { EntryIndex = 0, Name = "Bulleteer", IsLieutenant = true, Cost = 28 }]
            }
        }));

        var page = Render<LeagueCompanyPage>(parameters => parameters.Add(p => p.LeagueId, "l1").Add(p => p.SeasonId, "s2"));

        page.WaitForAssertion(() => Assert.Equal("Blue Company", page.Find("h1").TextContent));
        Assert.Contains("Bulleteer", page.Find(".trooper").TextContent, StringComparison.Ordinal);
    }
}
