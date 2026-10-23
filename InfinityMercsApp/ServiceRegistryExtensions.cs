using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Feedback;
using InfinityMercsApp.Services.Import;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Rolls;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Season.Inducements;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.Services.Wiki;
using InfinityMercsApp.ViewModels.About;
using InfinityMercsApp.ViewModels.Armory;
using InfinityMercsApp.ViewModels.Company;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.Feedback;
using InfinityMercsApp.ViewModels.Marketplaces;
using InfinityMercsApp.ViewModels.Reference;
using InfinityMercsApp.ViewModels.Season;
using InfinityMercsApp.ViewModels.Season.Bye;
using InfinityMercsApp.ViewModels.Season.Downtime;
using InfinityMercsApp.ViewModels.Season.Game;
using InfinityMercsApp.ViewModels.Season.Loot;
using InfinityMercsApp.ViewModels.Season.Overview;
using InfinityMercsApp.ViewModels.Season.RankUp;
using InfinityMercsApp.ViewModels.Settings;
using InfinityMercsApp.ViewModels.Splash;
using InfinityMercsApp.ViewModels.UnitBrowser;
using InfinityMercsApp.Views.About;
using InfinityMercsApp.Views.Armory;
using InfinityMercsApp.Views.Company;
using InfinityMercsApp.Views.Feedback;
using InfinityMercsApp.Views.Marketplaces;
using InfinityMercsApp.Views.Reference;
using InfinityMercsApp.Views.Season;
using InfinityMercsApp.Views.Season.Bye;
using InfinityMercsApp.Views.Season.Downtime;
using InfinityMercsApp.Views.Season.Game;
using InfinityMercsApp.Views.Season.Loot;
using InfinityMercsApp.Views.Season.Overview;
using InfinityMercsApp.Views.Season.RankUp;
using InfinityMercsApp.Views.Settings;
using InfinityMercsApp.Views.Splash;
using InfinityMercsApp.Views.UnitBrowser;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.ApiClient.Telemetry;
using Microsoft.Identity.Client;

namespace InfinityMercsApp;

public static class ServiceRegistryExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<SplashPageViewModel>()
                .AddTransient<SettingsViewModel>()
                .AddTransient<ArmoryViewModel>()
                .AddTransient<LoadSeasonViewModel>()
                .AddTransient<LoadCompanyViewModel>()
                .AddTransient<PerksTablesViewModel>()
                .AddTransient<MarketplacesViewModel>()
                .AddTransient<MarketplaceDetailViewModel>()
                .AddTransient<PlayModeViewModel>()
                .AddTransient<ExperienceViewModel>()
                .AddTransient<InjuriesViewModel>()
                .AddTransient<MercsGlossaryViewModel>()
                .AddTransient<UnitDetailViewModel>()
                .AddTransient<UnitEncyclopediaViewModel>()
                .AddTransient<CreateNewCompanyPageViewModel>()
                .AddTransient<StandardCompanySourcePopupPageViewModel>()
                .AddTransient<FeedbackBugsPageViewModel>()
                .AddTransient<AboutPageViewModel>()
                .AddTransient<MercsSeasonPageViewModel>()
                .AddTransient<MissionOutcomeViewModel>()
                .AddTransient<DowntimeViewModel>()
                .AddTransient<RankUpViewModel>()
                .AddTransient<GameModeViewModel>()
                .AddTransient<ByeRoundViewModel>()
                .AddTransient<LootBoxViewModel>()
                .AddTransient<SeasonViewModel>();

        return services;
    }

    public static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddTransient<AppShell>()
                .AddTransient<ArmoryPage>()
                .AddTransient<MercsGlossaryPage>()
                .AddTransient<CreateNewCompanyPage>()
                .AddTransient<StandardCompanySourcePopupPage>()
                .AddTransient<SplashPage>()
                .AddTransient<SettingsPage>()
                .AddTransient<UnitEncyclopediaPage>()
                .AddTransient<FeedbackBugsPage>()
                .AddTransient<AboutPage>()
                .AddTransient<MercsSeasonPage>()
                .AddTransient<LoadSeasonPage>()
                .AddTransient<LoadCompanyPage>()
                .AddTransient<PerksTablesPage>()
                .AddTransient<SeasonPage>()
                .AddTransient<PlayModePage>()
                .AddTransient<GameModePage>()
                .AddTransient<MissionOutcomePage>()
                .AddTransient<InjuriesPage>()
                .AddTransient<ExperiencePage>()
                .AddTransient<DowntimePage>()
                .AddTransient<RankUpPage>()
                .AddTransient<ByeRoundPage>()
                .AddTransient<LootBoxPage>()
                .AddTransient<MarketplacesPage>();

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, MauiNavigationService>()
                .AddSingleton(Launcher.Default)
                .AddTransient<CompanySelectionServices>()
                .AddSingleton<ICompanySelectionNavigator, CompanySelectionNavigator>()
                .AddSingleton<IArmyDataService, ArmyDataService>()
                .AddSingleton<IStoreProvider, StoreProvider>()
                .AddSingleton<StoreCatalog>()
                .AddSingleton<FactionLogoCacheService>()
                .AddSingleton<LogoBundleInstaller>()
                .AddSingleton<IFeedbackService, FeedbackService>()
                .AddSingleton(_ => new DeviceDetails(
                    AppInfo.Current.VersionString,
                    DeviceInfo.Current.Platform.ToString(),
                    $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}",
                    DeviceInfo.Current.Name))
                .AddSingleton<IArmyDataUpdater, ArmyDataUpdater>()
                .AddSingleton<ISeasonFileService, SeasonFileStore>()
                .AddSingleton<IDialogService, MauiDialogService>()
                .AddSingleton<ICaptainConfigurationDialog, MauiCaptainConfigurationDialog>()
                .AddSingleton(_ => RecordPaths.ForAppData(FileSystem.Current.AppDataDirectory))
                .AddSingleton<SeasonRecordCatalog>()
                .AddSingleton<CompanyRecordCatalog>()
                .AddSingleton<ICompanyFileService, CompanyFileStore>()
                .AddSingleton<CompanyFactionCatalog>()
                .AddSingleton<ThemeFactions>()
                .AddSingleton<UnitDetailsLoader>()
                .AddSingleton<CaptainOptionCatalog>()
                .AddSingleton<CaptainSetup>()
                .AddSingleton<SavedEntryWriter>()
                .AddSingleton<CompanyFounder>()
                .AddSingleton<CompanyRecruiter>()
                .AddSingleton<CoreFireteamEligibility>()
                .AddSingleton<SelectionUnitCatalog>()
                .AddSingleton<UnitVisibility>()
                .AddSingleton<UnitFilterOptionsCatalog>()
                .AddSingleton<CompanyRoster>()
                .AddTransient<PeripheralTrades>()
                .AddSingleton<DeploymentRoster>()
                .AddSingleton<HiredTroops>()
                .AddSingleton<MissionDebrief>()
                .AddSingleton<CaptainChange>()
                .AddSingleton<IDice, Dice>()
                .AddSingleton<IAppPackageFiles, AppPackageFiles>();

        services.AddSingleton<HttpClient>(_ =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InfinityMercsApp/1.0");
            return client;
        });
        services.AddSingleton<IWikiDescriptionService, WikiDescriptionService>();

        // Sign-in to the app's online side, with the Entra External ID tenant.
        services.AddSingleton(SignInSettings.Production)
                .AddSingleton(provider =>
                {
                    var settings = provider.GetRequiredService<SignInSettings>();
                    return PublicClientApplicationBuilder.Create(settings.ClientId)
                        .WithAuthority(settings.Authority)
#if WINDOWS
                        .WithRedirectUri(SignInSettings.DesktopRedirectUri)
#else
                        .WithRedirectUri(settings.RedirectUri)
#endif
                        .Build();
                })
                .AddSingleton<ISignInService, MsalSignInService>();

        // Companies and seasons kept online: every save goes in the outbox, which the coordinator sends.
        services.AddSingleton<SyncLedger>()
                .AddSingleton<ISyncOutbox>(provider => provider.GetRequiredService<SyncLedger>())
                .AddSingleton<DocumentSync>()
                .AddSingleton<ISyncCoordinator, SyncCoordinator>();

        // Telemetry: noted in an outbox on the device and sent to the app's own API, so no Application Insights key
        // ships in the app.
        services.AddSingleton(TimeProvider.System)
                .AddSingleton(_ => TelemetryPaths.ForAppData(FileSystem.Current.AppDataDirectory))
                .AddSingleton<TelemetryStore>()
                .AddSingleton<ITelemetryRecorder>(provider => provider.GetRequiredService<TelemetryStore>())
                .AddSingleton<ITelemetrySettings>(provider => provider.GetRequiredService<TelemetryStore>())
                .AddSingleton(_ => new TelemetrySource(
                    AppInfo.Current.VersionString,
                    DeviceInfo.Current.Platform.ToString(),
                    DeviceInfo.Current.VersionString,
                    $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}"))
                .AddSingleton(TelemetryEndpoint.Production)
                .AddSingleton<ITelemetryApi, TelemetryApiClient>()
                .AddSingleton<TelemetrySender>()
                .AddSingleton<ITelemetryCoordinator, TelemetryCoordinator>()
                .AddSingleton<PageTelemetry>();

        return services;
    }
}
