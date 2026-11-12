using Bunit;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Web.Layout;
using InfinityMercsApp.Web.Pages;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>The site, signed in, with a company founded and its season open: the start of every season step test.</summary>
public abstract class SeasonSiteContext : SiteContext
{
    protected SeasonSiteContext()
    {
        Authorization.SetAuthorized("Kestrel");
        Dialogs = Render<DialogHost>();
    }

    /// <summary>Where the site was last sent.</summary>
    protected string Address => Get<NavigationManager>().Uri;

    /// <summary>The dialog host, rendered before anything that asks.</summary>
    protected IRenderedComponent<DialogHost> Dialogs { get; }

    private protected async Task<FoundedSeason> OpenFoundedSeasonAsync()
    {
        FoundBlueLions();
        var season = Render<Season>();
        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".team-list .unit-row")), Patience);
        var record = (await Get<SeasonRecordCatalog>().ListAsync()).Single();
        return new FoundedSeason(season, record.SeasonIdentifier);
    }

    /// <summary>The season's file as it is now.</summary>
    protected async Task<SeasonFile?> SeasonFileAsync()
    {
        var record = (await Get<SeasonRecordCatalog>().ListAsync()).Single();
        return await Get<ISeasonFileService>().LoadAsync(record.SeasonFilePath);
    }

    /// <summary>Gives every trooper <paramref name="xp"/> XP in a contract already played.</summary>
    protected async Task GiveEveryoneXpAsync(int xp)
    {
        var record = (await Get<SeasonRecordCatalog>().ListAsync()).Single();
        var company = await Get<ICompanyFileService>().LoadAsync(record.CompanyFilePath);
        var results = company?.Entries.Select(entry => new SeasonMissionUnitResult { EntryIndex = entry.EntryIndex, UnitName = entry.Name, XpGained = xp }).ToList() ?? [];
        await Get<ISeasonFileService>().UpdateAsync(record.SeasonFilePath, season => season.Rounds.Add(new SeasonRound
        {
            RoundIndex = 1,
            MissionResults = new SeasonMissionResult { UnitResults = results }
        }));
    }

    /// <summary>Gives the company's captain <paramref name="injury"/>, carried into the next contract.</summary>
    protected async Task InjureCaptainAsync(Injury injury)
    {
        var record = (await Get<SeasonRecordCatalog>().ListAsync()).Single();
        var company = await Get<ICompanyFileService>().LoadAsync(record.CompanyFilePath);
        var captain = company?.Entries.Single(entry => entry.IsLieutenant).EntryIndex ?? 0;
        await Get<ISeasonFileService>().UpdateAsync(record.SeasonFilePath, season => season.TrooperFor(captain).Injuries.Add(injury));
    }

    /// <summary>Confirms (or takes the first choice of) the dialog asked next.</summary>
    protected void Answer() => Dialogs.WaitForAssertion(() => Dialogs.Find(".dialog button.primary").Click(), Patience);

    /// <summary>Confirms whatever is asked now, if anything is.</summary>
    protected void AnswerAnyQuestion()
    {
        if (Dialogs.FindAll(".dialog button.primary").Count > 0)
        {
            Dialogs.Find(".dialog button.primary").Click();
        }
    }
}
