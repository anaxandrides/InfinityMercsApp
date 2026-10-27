using InfinityMercsApp.Infrastructure.API.InfinityArmy;

namespace InfinityMercsApp.Api.ArmyData.Prebuilt;

/// <summary>The payloads a refresh just copied, read again from memory to build the generated companies from them.</summary>
public sealed class CopiedPayloads(string metadataJson, IReadOnlyDictionary<int, string> armyJson) : IArmyPayloadSource
{
    public IReadOnlyCollection<int> FactionIds => armyJson.Keys.ToList();

    public Task<string> GetMetadataAsync(CancellationToken cancellationToken = default) => Task.FromResult(metadataJson);

    public Task<string> GetArmyAsync(int factionId, CancellationToken cancellationToken = default) =>
        armyJson.TryGetValue(factionId, out var json)
            ? Task.FromResult(json)
            : throw new KeyNotFoundException($"The refresh copied no army for faction {factionId}.");
}
