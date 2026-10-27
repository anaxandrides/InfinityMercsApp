using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;
using InfinityMercsApp.UnitTests.Characterization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json;

/// <summary>An in-memory army store with the reference metadata, and the generated companies' generators over it.</summary>
internal sealed class StoredArmies
{
    private StoredArmies(InMemoryRepository repository)
    {
        var content = new ArmyContentStore(repository);
        Factions = new FactionProvider(repository, content);
        Import = new ArmyImportProvider(repository, content);
        Metadata = new MetadataProvider(repository);
        Generators =
        [
            new AirborneCompanyFactionGenerator(repository, Factions, Import),
            new InspiringCompanyFactionGenerator(repository, Factions, Import),
            new TagCompanyFactionGenerator(repository, Factions, Import, ArmyJsonReader.Shared)
        ];
    }

    public FactionProvider Factions { get; }

    public ArmyImportProvider Import { get; }

    public IReadOnlyList<ICompanyFactionGenerator> Generators { get; }

    public MetadataProvider Metadata { get; }

    public static async Task<StoredArmies> CreateAsync()
    {
        var payloads = Substitute.For<IArmyPayloadSource>();
        payloads.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(File.ReadAllText(RepoFiles.Resolve("InfinityMercsApp/ReferenceData/metadata.json")));
        var api = new InfinityArmyAPI(payloads, NullLogger<InfinityArmyAPI>.Instance, ArmyJsonReader.Shared);
        var armies = new StoredArmies(new InMemoryRepository());
        armies.Metadata.Import(await api.GetMetaDataAsync() ?? throw new InvalidOperationException("No metadata."));
        return armies;
    }
}
