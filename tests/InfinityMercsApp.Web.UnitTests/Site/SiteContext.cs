using Bunit;
using AngleSharp.Dom;
using Bunit.TestDoubles;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Infrastructure;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Links;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.Web.ArmyData;
using InfinityMercsApp.Web.Files;
using InfinityMercsApp.Web.Layout;
using InfinityMercsApp.Web.Pages;
using InfinityMercsApp.Web.Platform;
using InfinityMercsApp.Web.UnitTests.ArmyData;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>
/// The site's services as Program.cs puts them together, over the reference metadata and PanOceania army, with the
/// account's API, sign-in, sync and the browser faked and the site's files in a folder of their own.
/// </summary>
public abstract class SiteContext : BunitContext
{
    private readonly string _folder = Directory.CreateTempSubdirectory("site-").FullName;

    /// <param name="windowWidth">How wide the browser window is, which decides how the screens lay out.</param>
    protected SiteContext(double windowWidth = 1600)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./js/viewport.js").Setup<double>("watch", _ => true).SetResult(windowWidth);
        Authorization = AddAuthorization();
        Paths = RecordPaths.ForAppData(_folder);
        Tokens.GetAsync().Returns("token-1");

        var armies = new WebArmyDataKit();
        Services
            .AddInfrastructureServices()
            .AddMercsServices()
            .AddViewModels();
        Services.Replace(ServiceDescriptor.Singleton<ISQLiteRepository, InMemoryRepository>());
        Services.Replace(ServiceDescriptor.Singleton(new ArmyMirrorClient(new HttpClient(armies.Api), new ArmyMirrorEndpoint(new Uri("https://api.example/api/")))));
        Services
            .AddSingleton<BrowserPayloadCache>()
            .AddSingleton<CachedArmyPayloads>()
            .AddSingleton<WebArmyData>();
        Services.Replace(ServiceDescriptor.Singleton<IArmyPayloadSource>(provider => provider.GetRequiredService<CachedArmyPayloads>()));
        Services.Replace(ServiceDescriptor.Singleton<IArmyDataLoader>(provider => provider.GetRequiredService<WebArmyData>()));
        Services.AddSingleton<RemoteLogoStore>();
        Services.Replace(ServiceDescriptor.Singleton<ILogoStore>(provider => provider.GetRequiredService<RemoteLogoStore>()));
        Services.Replace(ServiceDescriptor.Singleton<ILogoBundleInstaller, NoLogoBundle>());
        Services.Replace(ServiceDescriptor.Singleton(Sync));
        Services.Replace(ServiceDescriptor.Singleton(Api));

        Services
            .AddSingleton(new HttpClient())
            .AddSingleton(Paths)
            .AddSingleton<LogoUrls>()
            .AddSingleton<CompanySelectionSession>()
            .AddSingleton<NavigationQuery>()
            .AddSingleton<WebDialogService>()
            .AddSingleton<IDialogService>(provider => provider.GetRequiredService<WebDialogService>())
            .AddSingleton<WebCaptainConfigurationDialog>()
            .AddSingleton<ICaptainConfigurationDialog>(provider => provider.GetRequiredService<WebCaptainConfigurationDialog>())
            .AddSingleton<INavigationService, WebNavigationService>()
            .AddSingleton<ICompanySelectionNavigator, WebCompanySelectionNavigator>()
            .AddSingleton<ILinkLauncher, WebLinkLauncher>()
            .AddSingleton(PackageFiles)
            .AddSingleton(TimeProvider.System)
            .AddSingleton(Tokens)
            .AddSingleton<ISignInService, WebSignInService>()
            .AddSingleton<UserFiles>();

        Services
            .AddSingleton(TelemetryPaths.ForAppData(_folder))
            .AddSingleton<TelemetryStore>()
            .AddSingleton<ITelemetryRecorder>(provider => provider.GetRequiredService<TelemetryStore>())
            .AddSingleton<ITelemetrySettings>(provider => provider.GetRequiredService<TelemetryStore>())
            .AddSingleton(new TelemetrySource("test", "Web", string.Empty, string.Empty))
            .AddSingleton(Substitute.For<ITelemetryApi>())
            .AddSingleton<TelemetrySender>()
            .AddSingleton(Telemetry)
            .AddSingleton<BrowserTelemetry>();
    }

    protected BunitAuthorizationContext Authorization { get; }

    protected RecordPaths Paths { get; }

    protected IUserDocumentsApi Api { get; } = Substitute.For<IUserDocumentsApi>();

    protected IAccessTokens Tokens { get; } = Substitute.For<IAccessTokens>();

    protected ISyncCoordinator Sync { get; } = Substitute.For<ISyncCoordinator>();

    protected IAppPackageFiles PackageFiles { get; } = new RepoPackageFiles();

    protected ITelemetryCoordinator Telemetry { get; } = Substitute.For<ITelemetryCoordinator>();

    protected static TimeSpan Patience { get; } = TimeSpan.FromSeconds(20);

    protected T Get<T>()
        where T : notnull => Services.GetRequiredService<T>();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    /// <summary>Opens the builder for a vanilla company and picks PanOceania, the army the fake API serves.</summary>
    protected IRenderedComponent<CompanySelection> BuildPanOceania()
    {
        var source = Render<CompanySource>();
        source.WaitForAssertion(() => source.FindAll(".company-choice")[0].Click(), Patience);
        source.WaitForAssertion(() => Assert.NotNull(Get<CompanySelectionSession>().Current), Patience);

        var page = Render<CompanySelection>();
        page.WaitForAssertion(() => PanOceaniaTile(page).Click(), Patience);
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".unit-row")), Patience);
        return page;
    }

    private static IElement PanOceaniaTile(IRenderedComponent<CompanySelection> page) =>
        page.FindAll(".faction-tile").First(tile => tile.TextContent.Contains("PanOceania", StringComparison.Ordinal));

    /// <summary>Looks through the units for a profile with Lieutenant and hires it.</summary>
    protected static void HireLieutenant(IRenderedComponent<CompanySelection> page)
    {
        for (var index = 0; index < page.FindAll(".unit-row").Count; index++)
        {
            page.FindAll(".unit-row")[index].Click();
            page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".profile-card")), Patience);
            var lieutenant = page.FindAll(".profile-card")
                .FirstOrDefault(card => !card.ClassList.Contains("blocked") && card.TextContent.Contains("Lieutenant", StringComparison.Ordinal));
            if (lieutenant is not null)
            {
                lieutenant.Click();
                page.WaitForAssertion(() => Assert.Single(page.FindAll(".entry-card")), Patience);
                return;
            }
        }

        Assert.Fail("No unit offers a lieutenant profile.");
    }

    /// <summary>Founds "Blue Lions" with a lieutenant captain named Kestrel; the season it opens is held for the season page.</summary>
    protected void FoundBlueLions()
    {
        var captainHost = Render<CaptainHost>();
        var page = BuildPanOceania();
        HireLieutenant(page);
        page.Find("#company-name").Input("Blue Lions");
        page.WaitForAssertion(() => page.Find(".start button.primary").Click(), Patience);
        captainHost.WaitForAssertion(() => captainHost.Find(".buttons button.primary").Click(), Patience);
        page.WaitForAssertion(() => Assert.EndsWith("/season", Get<NavigationManager>().Uri, StringComparison.Ordinal), Patience);
    }
}
