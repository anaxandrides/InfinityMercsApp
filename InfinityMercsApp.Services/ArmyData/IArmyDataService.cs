using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Rolls;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Services.ArmyData;

/// <summary>
/// Shared backend facade for metadata/army read operations used by UI screens.
/// </summary>
public interface IArmyDataService
{
    IReadOnlyList<FactionRecord> GetMetadataFactions(bool includeDiscontinued = false, CancellationToken cancellationToken = default);
    FactionRecord? GetMetadataFactionById(int id, CancellationToken cancellationToken = default);
    ArmyFactionRecord? GetFactionSnapshot(int factionId, CancellationToken cancellationToken = default);
    IReadOnlyList<ArmyResumeRecord> GetResumeByFaction(int factionId, CancellationToken cancellationToken = default);
    IReadOnlyList<ArmyResumeRecord> GetResumeByFactionMercsOnly(int factionId, CancellationToken cancellationToken = default);
    ArmyUnitRecord? GetUnit(int factionId, int unitId, CancellationToken cancellationToken = default);
    IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(IReadOnlyCollection<int> factionIds, CancellationToken cancellationToken = default);
    /// <summary>A recruit table's results by d20 range, lowest first; empty when the metadata has none.</summary>
    IReadOnlyList<RollRange> GetRecruitTable(RecruitRollTable table);

    /// <summary>Every weapon's name in the metadata.</summary>
    IReadOnlyList<string> GetWeaponNames();

    /// <summary>Every skill's name in the metadata.</summary>
    IReadOnlyList<string> GetSkillNames();

    /// <summary>Movement in the user's chosen units (inches or cm); "-" when there is none.</summary>
    string FormatMovement(Movement? movement);

    /// <summary>As <see cref="FormatMovement"/>, from the two values in cm.</summary>
    string FormatMoveValue(int? firstCm, int? secondCm) => FormatMovement(Movement.From(firstCm, secondCm));
}
