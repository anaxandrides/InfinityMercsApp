using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Infrastructure;

public static class ServiceRegistryExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register all the related services here
        services.AddSingleton<ISQLiteRepository, SQLiteRepository>()
                .AddSingleton<IArmyContentStore, ArmyContentStore>()
                .AddSingleton<IInfinityArmyAPI, InfinityArmyAPI>()
                // Army data comes from the app's own copy, and from Corvus Belli when the copy can't serve it.
                .AddSingleton(ArmyMirrorEndpoint.Production)
                .AddSingleton<ArmyMirrorClient>()
                .AddSingleton<IArmyMirror>(provider => provider.GetRequiredService<ArmyMirrorClient>())
                .AddSingleton<CorvusBelliPayloadSource>()
                .AddSingleton<IArmyPayloadSource>(provider => new MirrorFirstPayloadSource(
                    provider.GetRequiredService<ArmyMirrorClient>(),
                    provider.GetRequiredService<CorvusBelliPayloadSource>(),
                    TimeProvider.System))
                // The signed-in user's companies and seasons, kept by the same API.
                .AddSingleton(UserDocumentsEndpoint.Production)
                .AddSingleton<IUserDocumentsApi, UserDocumentsClient>()
                .AddSingleton<IAppSettingsProvider, AppSettingsProvider>()
                .AddSingleton<IArmyImportProvider, ArmyImportProvider>()
                .AddSingleton<IFactionProvider, FactionProvider>()
                .AddSingleton<IMetadataProvider, MetadataProvider>()
                .AddSingleton<ICohesiveCompanyFactionQueryProvider, CohesiveCompanyFactionQueryProvider>()
                .AddSingleton<ISpecOpsProvider, SpecOpsProvider>()
                .AddSingleton<IInspiringCompanyFactionGenerator, InspiringCompanyFactionGenerator>()
                .AddSingleton<IAirborneCompanyFactionGenerator, AirborneCompanyFactionGenerator>()
                .AddSingleton<ITagCompanyFactionGenerator, TagCompanyFactionGenerator>()
                // Parses Army API payloads into typed models during import.
                .AddSingleton<IArmyJsonReader>(ArmyJsonReader.Shared);
        return services;
    }
}
