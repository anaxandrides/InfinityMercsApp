using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Diagnostics;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.Season;
using InfinityMercsApp.Views.UnitEncyclopedia;

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
                .AddTransient<PlayModeViewModel>()
                .AddTransient<DebugViewModel>()
                .AddTransient<ExperienceViewModel>()
                .AddTransient<InjuriesViewModel>()
                .AddTransient<MercsGlossaryViewModel>()
                .AddTransient<UnitDetailViewModel>()
                .AddTransient<UnitEncyclopediaViewModel>()
                .AddTransient<PerkTesterViewModel>()
                .AddTransient<CreateNewCompanyPageViewModel>()
                .AddTransient<StandardCompanySourcePopupPageViewModel>()
                .AddTransient<FeedbackBugsPageViewModel>()
                .AddTransient<AboutPageViewModel>()
                .AddTransient<MercsSeasonPageViewModel>()
                .AddTransient<MissionOutcomeViewModel>();

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
                .AddTransient<DebugPage>()
                .AddTransient<PerkTesterPage>()
                .AddTransient<CompanyViewerPage>()
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
                .AddTransient<MarketplacesPage>();

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, MauiNavigationService>()
                .AddSingleton(Launcher.Default)
                .AddSingleton<ICompanySelectionNavigator, CompanySelectionNavigator>()
                .AddSingleton<IArmyDataService, ArmyDataService>()
                .AddSingleton<IStoreProvider, StoreProvider>()
                .AddSingleton<FactionLogoCacheService>()
                .AddSingleton<IFeedbackService, FeedbackService>()
                .AddSingleton<IImportService, ImportService>()
                .AddSingleton<ISeasonFileService, SeasonFileStore>()
                .AddSingleton<IDialogService, MauiDialogService>()
                .AddSingleton(_ => RecordPaths.ForAppData(FileSystem.Current.AppDataDirectory))
                .AddSingleton<SeasonRecordCatalog>()
                .AddSingleton<CompanyRecordCatalog>()
                .AddSingleton<DeploymentRoster>()
                .AddSingleton<TestCompanyReformer>()
                .AddSingleton<MissionDebrief>()
                .AddSingleton<IDice, Dice>()
                .AddSingleton<IAppPackageFiles, AppPackageFiles>();

        services.AddSingleton<HttpClient>(_ =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InfinityMercsApp/1.0");
            return client;
        });
        services.AddSingleton<IWikiDescriptionService, WikiDescriptionService>();

        return services;
    }
}
