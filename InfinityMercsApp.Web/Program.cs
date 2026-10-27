using System.Reflection;
using InfinityMercsApp;
using InfinityMercsApp.ApiClient;
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
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.Web;
using InfinityMercsApp.Web.ArmyData;
using InfinityMercsApp.Web.Files;
using InfinityMercsApp.Web.Platform;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Signing in: the Entra External ID tenant, through the same app registration as the phone app (wwwroot/appsettings.json).
var api = builder.Configuration.GetSection("Api").Get<ApiSettings>() ?? new ApiSettings();
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("Entra", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(api.Scope);
    options.ProviderOptions.LoginMode = "redirect";
});

var apiAddress = string.IsNullOrWhiteSpace(api.BaseAddress) ? ApiAddress.Production : new Uri(api.BaseAddress);

// The shared services and view models, as the phone app has them. The army data lives in memory rather than SQLite,
// read from the API's copy only (Corvus Belli's API doesn't answer browsers on other sites), and the companies and
// seasons are files in the browser's memory, kept in the account by the same sync as the app's.
builder.Services
    .AddInfrastructureServices()
    .AddMercsServices()
    .AddViewModels();
builder.Services.Replace(ServiceDescriptor.Singleton<ISQLiteRepository, InMemoryRepository>());
builder.Services.Replace(ServiceDescriptor.Singleton(new ArmyMirrorEndpoint(apiAddress)));
// Payloads come from the browser's cache when kept there, else from the API's copy; armies load as pages need them.
builder.Services
    .AddSingleton<BrowserPayloadCache>()
    .AddSingleton<CachedArmyPayloads>()
    .AddSingleton<WebArmyData>();
builder.Services.Replace(ServiceDescriptor.Singleton<IArmyPayloadSource>(provider => provider.GetRequiredService<CachedArmyPayloads>()));
builder.Services.Replace(ServiceDescriptor.Singleton<IArmyDataLoader>(provider => provider.GetRequiredService<WebArmyData>()));
builder.Services.Replace(ServiceDescriptor.Singleton(new UserDocumentsEndpoint(apiAddress)));
builder.Services.AddSingleton<RemoteLogoStore>();
builder.Services.Replace(ServiceDescriptor.Singleton<ILogoStore>(provider => provider.GetRequiredService<RemoteLogoStore>()));
builder.Services.Replace(ServiceDescriptor.Singleton<ILogoBundleInstaller, NoLogoBundle>());

// The site's own platform services.
builder.Services
    .AddSingleton(_ => new HttpClient())
    .AddSingleton(RecordPaths.ForAppData("/mercs"))
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
    .AddSingleton<IAppPackageFiles, WebPackageFiles>()
    .AddSingleton(TimeProvider.System)
    .AddSingleton<IAccessTokens, MsalAccessTokens>()
    .AddSingleton<ISignInService, WebSignInService>()
    .AddSingleton<IUserDocumentsApi, UserDocumentsClient>()
    .AddSingleton<UserFiles>();

// Telemetry, as the app sends it: noted in an outbox, sent to the API every few minutes and when the tab is hidden.
builder.Services
    .AddSingleton(TelemetryPaths.ForAppData("/"))
    .AddSingleton<TelemetryStore>()
    .AddSingleton<ITelemetryRecorder>(provider => provider.GetRequiredService<TelemetryStore>())
    .AddSingleton<ITelemetrySettings>(provider => provider.GetRequiredService<TelemetryStore>())
    .AddSingleton(new TelemetrySource(
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "web",
        "Web",
        string.Empty,
        string.Empty))
    .AddSingleton(new TelemetryEndpoint(apiAddress))
    .AddSingleton<ITelemetryApi>(provider => new TelemetryApiClient(new HttpClient(new KeepAliveHandler()), provider.GetRequiredService<TelemetryEndpoint>()))
    .AddSingleton<TelemetrySender>()
    .AddSingleton<ITelemetryCoordinator, TelemetryCoordinator>()
    .AddSingleton<BrowserTelemetry>()
    .AddSingleton<ILoggerProvider, TelemetryLoggerProvider>();

var host = builder.Build();
await host.Services.GetRequiredService<BrowserTelemetry>().StartAsync();
await host.RunAsync();
