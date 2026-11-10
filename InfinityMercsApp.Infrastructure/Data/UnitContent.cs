using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.SpecOps;

namespace InfinityMercsApp.Infrastructure.Data;

/// <summary>
/// The typed content of one unit (an army unit or a spec-ops unit): its profile groups, the options
/// declared on the unit itself, its own filter ids and the armies it belongs to, and a Spec-Ops or Team-Ops unit's
/// upgrade table.
/// </summary>
public sealed record UnitContent(
    UnitProfileGroups ProfileGroups,
    IReadOnlyList<ProfileOption> Options,
    UnitFilterIds Filters,
    IReadOnlyList<int> FactionIds)
{
    public static UnitContent Empty { get; } = new(UnitProfileGroups.Empty, [], UnitFilterIds.Empty, []);

    public SpecUpgradeTable Upgrades { get; init; } = SpecUpgradeTable.Empty;
}
