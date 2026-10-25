using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Feedback;
using InfinityMercsApp.Services.Links;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Telemetry;
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
using Microsoft.Identity.Client;

namespace InfinityMercsApp;

public static class ServiceRegistryExtensions
{
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
        // What the app and the web site share (InfinityMercsApp.ViewModels), then this app's own platform services.
        services.AddMercsServices()
                .AddSingleton<INavigationService, MauiNavigationService>()
                .AddSingleton(Launcher.Default)
                .AddSingleton<ILinkLauncher, MauiLinkLauncher>()
                .AddSingleton<ICompanySelectionNavigator, CompanySelectionNavigator>()
                .AddSingleton(_ => new DeviceDetails(
                    AppInfo.Current.VersionString,
                    DeviceInfo.Current.Platform.ToString(),
                    $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}",
                    DeviceInfo.Current.Name))
                .AddSingleton<IDialogService, MauiDialogService>()
                .AddSingleton<ICaptainConfigurationDialog, MauiCaptainConfigurationDialog>()
                .AddSingleton(_ => RecordPaths.ForAppData(FileSystem.Current.AppDataDirectory))
                .AddSingleton<IAppPackageFiles, AppPackageFiles>();

        services.AddSingleton<HttpClient>(_ =>
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("InfinityMercsApp/1.0");
            return client;
        });

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
