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
using InfinityMercsApp.Services.Feedback;
using InfinityMercsApp.Services.Import;
using InfinityMercsApp.Services.Logos;
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
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp;

/// <summary>
/// The services and view models the MAUI app and the web site share. Each adds its own platform services on top:
/// navigation, dialogs, links, where records are kept, sign-in and the HTTP client.
/// </summary>
public static class MercsServiceRegistry
{
    /// <summary>The company, season, army data and sync services.</summary>
    public static IServiceCollection AddMercsServices(this IServiceCollection services)
    {
        services.AddTransient<CompanySelectionServices>()
                .AddTransient<CompanySelectionScreens>()
                .AddSingleton<IArmyDataService, ArmyDataService>()
                .AddSingleton<IArmyDataLoader, StoredArmyData>()
                .AddSingleton<IStoreProvider, StoreProvider>()
                .AddSingleton<StoreCatalog>()
                .AddSingleton<FactionLogoCacheService>()
                .AddSingleton<ILogoStore, LogoFileStore>()
                .AddSingleton<ILogoBundleInstaller, LogoBundleInstaller>()
                .AddSingleton<IFeedbackService, FeedbackService>()
                .AddSingleton<IArmyDataUpdater, ArmyDataUpdater>()
                .AddSingleton<ISeasonFileService, SeasonFileStore>()
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
                .AddSingleton<IWikiDescriptionService, WikiDescriptionService>()
                // Companies and seasons kept online: every save goes in the outbox, which the platform's coordinator sends.
                .AddSingleton<SyncLedger>()
                .AddSingleton<ISyncOutbox>(provider => provider.GetRequiredService<SyncLedger>())
                .AddSingleton<DocumentSync>()
                .AddSingleton<ISyncCoordinator, SyncCoordinator>();

        return services;
    }

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
}
