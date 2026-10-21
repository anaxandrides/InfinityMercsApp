using InfinityMercsApp.Domain.Models.Army.SpecOps;

namespace InfinityMercsApp.UnitTests.Storage;

/// <summary>Everything stored for an army's spec-ops, to dump and compare in one go.</summary>
internal sealed record StoredSpecopsDump(
    IReadOnlyList<SpecopsSkill> Skills,
    IReadOnlyList<SpecopsEquipment> Equipment,
    IReadOnlyList<SpecopsWeapon> Weapons,
    IReadOnlyList<SpecopsUnit> Units);
