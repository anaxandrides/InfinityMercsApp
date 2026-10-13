using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Providers;
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
