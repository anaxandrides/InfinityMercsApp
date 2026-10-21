namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

/// <summary>The army API's payloads exactly as served, for keeping a copy of them.</summary>
public interface IArmyPayloadSource
{
    /// <summary>The metadata payload: factions, weapons, skills and equipment.</summary>
    Task<string> GetMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>The army payload of <paramref name="factionId"/>.</summary>
    Task<string> GetArmyAsync(int factionId, CancellationToken cancellationToken = default);
}
