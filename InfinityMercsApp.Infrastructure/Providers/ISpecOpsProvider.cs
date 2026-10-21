using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.SpecOps;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// An interface to handle interactions with SpecOps
/// </summary>
public interface ISpecOpsProvider
{
    /// <summary>
    /// Gets all SpecOps skills for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<SpecopsSkill> GetSpecopsSkillsByFaction(int factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all SpecOps equipment for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<SpecopsEquipment> GetSpecopsEquipsByFaction(int factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all SpecOps weapons for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<SpecopsWeapon> GetSpecopsWeaponsByFaction(int factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all SpecOps units for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<SpecopsUnit> GetSpecopsUnitsByFaction(int factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached cohesive-company fireteam validity rows for one filter key and faction set.
    /// </summary>
    /// <param name="filterKey"></param>
    /// <param name="factionIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IReadOnlyList<CCFactionFireteamValidityRecord> GetCCFactionFireteamValidity(
        string filterKey,
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a cohesive-company fireteam validity cache row.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="filterKey"></param>
    /// <param name="hasValidCoreFireteams"></param>
    /// <param name="validCoreFireteams">The CORE fireteam names found valid.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    void UpsertCCFactionFireteamValidity(
        int factionId,
        string filterKey,
        bool hasValidCoreFireteams,
        IReadOnlyList<string> validCoreFireteams,
        CancellationToken cancellationToken = default);
}

