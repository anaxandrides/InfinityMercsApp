using InfinityMercsApp.Infrastructure.API.InfinityArmy;

namespace InfinityMercsApp.AotSmokeTests.Support;

/// <summary>The reference metadata and armies checked into the repository, served as Corvus Belli's API would.</summary>
public sealed class ReferencePayloads(RepoFolder repo) : IArmyPayloadSource
{
    public Task<string> GetMetadataAsync(CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(repo.PathOf("InfinityMercsApp/ReferenceData/metadata.json"), cancellationToken);

    public Task<string> GetArmyAsync(int factionId, CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(repo.PathOf($"InfinityMercsApp/ReferenceData/army-{factionId}.json"), cancellationToken);
}
