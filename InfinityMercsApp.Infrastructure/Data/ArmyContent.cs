using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Fireteams;

namespace InfinityMercsApp.Infrastructure.Data;

/// <summary>The typed content of one army: its filter catalogue, unit relations and fireteam chart.</summary>
public sealed record ArmyContent(ArmyFilterCatalog Filters, IReadOnlyList<ArmyRelation> Relations, FireteamChart FireteamChart)
{
    public static ArmyContent Empty { get; } = new(ArmyFilterCatalog.Empty, [], FireteamChart.Empty);
}
