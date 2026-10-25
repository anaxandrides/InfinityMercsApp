using System.Text.Json;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Factions;
using Microsoft.Extensions.Logging;

namespace InfinityMercsApp.Api.ArmyData.Prebuilt;

/// <summary>
/// Works out ahead, from a snapshot's armies, what the web site would otherwise work out in the browser by reading every
/// army: the Airborne, Inspiring and TAG companies' factions (built as the app builds them), and which factions the
/// Airborne, Inspiring and Cohesive companies list (with the same checks the selection screens make).
/// </summary>
public sealed class PrebuiltArmyBuilder(IArmyPayloadStore payloads, ILogger<InfinityArmyAPI> apiLogger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>What <paramref name="copied"/> builds, each kept under its hash.</summary>
    public async Task<PrebuiltArmyData> BuildAsync(CopiedPayloads copied, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var api = new InfinityArmyAPI(copied, apiLogger, ArmyJsonReader.Shared);
        var repository = new InMemoryRepository();
        var content = new ArmyContentStore(repository);
        var factions = new FactionProvider(repository, content);
        var import = new ArmyImportProvider(repository, content);
        var metadata = new MetadataProvider(repository);
        metadata.Import(await api.GetMetaDataAsync(cancellationToken) ?? throw new InvalidDataException("The copied metadata didn't read."));
        foreach (var factionId in copied.FactionIds.Order())
        {
            if (await api.GetArmyDataAsync(factionId, cancellationToken) is { } army)
            {
                import.Import(factionId, army, cancellationToken);
            }
        }

        IReadOnlyList<ICompanyFactionGenerator> generators =
        [
            new AirborneCompanyFactionGenerator(repository, factions, import),
            new InspiringCompanyFactionGenerator(repository, factions, import),
            new TagCompanyFactionGenerator(repository, factions, import, ArmyJsonReader.Shared)
        ];

        var generated = new List<MirroredArmy>();
        foreach (var generator in generators)
        {
            if (generator.Build(cancellationToken) is not { } army)
            {
                continue;
            }

            var hash = await KeepAsync(ArmyDefinitionJson.Write(army), cancellationToken);
            generated.Add(new MirroredArmy(generator.FactionId, army.Version, hash, now));
        }

        var listings = await new FactionListingsBuilder(ArmyDataServices.Over(metadata, factions)).BuildAsync(cancellationToken);
        var listingsHash = await KeepAsync(JsonSerializer.Serialize(listings, JsonOptions), cancellationToken);
        return new PrebuiltArmyData(generated, new MirroredPayload(listingsHash, now));
    }

    private async Task<string> KeepAsync(string json, CancellationToken cancellationToken)
    {
        var hash = ArmyPayloads.Hash(json);
        await payloads.SaveAsync(hash, json, cancellationToken);
        return hash;
    }
}
