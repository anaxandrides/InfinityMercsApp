using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Services;
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
        services.AddTransient<ModeSelectionViewModel>()
                .AddTransient<SplashPageViewModel>()
                .AddTransient<MainViewModel>()
                .AddTransient<ViewerViewModel>()
                .AddTransient<CreateNewCompanyPageViewModel>()
                .AddTransient<StandardCompanySourcePopupPageViewModel>()
                .AddTransient<TagCompanySourcePopupPageViewModel>()
                .AddTransient<LoneWolfCompanySourcePopupPageViewModel>()
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
                .AddTransient<ModeSelectionPage>()
                .AddTransient<CreateNewCompanyPage>()
                .AddTransient<StandardCompanySourcePopupPage>()
                .AddTransient<TagCompanySourcePopupPage>()
                .AddTransient<LoneWolfCompanySourcePopupPage>()
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
                .AddTransient<SeasonPage>()
                .AddTransient<PlayModePage>()
                .AddTransient<GameModePage>()
                .AddTransient<MissionOutcomePage>()
                .AddTransient<ExperiencePage>()
                .AddTransient<DowntimePage>()
                .AddTransient<MarketplacesPage>();

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, MauiNavigationService>()
                .AddSingleton(Launcher.Default)
                .AddSingleton<ICompanySelectionPageFactory, CompanySelectionPageFactory>()
                .AddSingleton<IArmyDataService, ArmyDataService>()
                .AddSingleton<IStoreProvider, StoreProvider>()
                .AddSingleton<FactionLogoCacheService>()
                .AddSingleton<IFeedbackService, FeedbackService>()
                .AddSingleton<IImportService, ImportService>()
                .AddSingleton<ISeasonFileService, SeasonFileStore>();

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
