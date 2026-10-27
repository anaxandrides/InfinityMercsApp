using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;

namespace InfinityMercsApp.Services.ArmyData;

/// <summary>Makes an <see cref="IArmyDataService"/> outside the apps' service registrations, as the API needs.</summary>
public static class ArmyDataServices
{
    /// <summary>The army data service over these providers.</summary>
    public static IArmyDataService Over(IMetadataProvider metadata, IFactionProvider factions) =>
        new ArmyDataService(metadata, factions, new CohesiveCompanyFactionQueryProvider(factions), appSettingsProvider: null);
}
