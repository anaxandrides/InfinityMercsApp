using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.UnitTests.Characterization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Infrastructure.Repositories;

/// <summary>The reference metadata and PanOceania army, imported into a repository as the app imports them, generated companies included.</summary>
internal sealed class ImportedArmyData
{
    public const int PanOceania = 101;

    private ImportedArmyData(ISQLiteRepository repository)
    {
        var content = new ArmyContentStore(repository);
        Factions = new FactionProvider(repository, content);
        Metadata = new MetadataProvider(repository);
        SpecOps = new SpecOpsProvider(repository, content);
        Import = new ArmyImportProvider(repository, content);
    }

    public FactionProvider Factions { get; }

    public MetadataProvider Metadata { get; }

    public SpecOpsProvider SpecOps { get; }

    private ArmyImportProvider Import { get; }

    public static async Task<ImportedArmyData> IntoAsync(ISQLiteRepository repository)
    {
        var payloads = Substitute.For<IArmyPayloadSource>();
        payloads.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(File.ReadAllText(RepoFiles.Resolve("InfinityMercsApp/ReferenceData/metadata.json")));
        payloads.GetArmyAsync(PanOceania, Arg.Any<CancellationToken>()).Returns(File.ReadAllText(RepoFiles.Resolve(ReferenceCorpus.Army101Path)));
        var api = new InfinityArmyAPI(payloads, NullLogger<InfinityArmyAPI>.Instance, ArmyJsonReader.Shared);

        var data = new ImportedArmyData(repository);
        data.Metadata.Import(await api.GetMetaDataAsync() ?? throw new InvalidOperationException("The reference metadata didn't read."));
        data.Import.Import(PanOceania, await api.GetArmyDataAsync(PanOceania) ?? throw new InvalidOperationException("The reference army didn't read."));
        new AirborneCompanyFactionGenerator(repository, data.Factions, data.Import).Generate();
        new InspiringCompanyFactionGenerator(repository, data.Factions, data.Import).Generate();
        new TagCompanyFactionGenerator(repository, data.Factions, data.Import, ArmyJsonReader.Shared).Generate();
        return data;
    }
}
