using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Fireteams;
using InfinityMercsApp.Domain.Models.Army.SpecOps;

namespace InfinityMercsApp.Domain.Models.Army;

/// <summary>
/// A whole army as the Infinity Army API describes it, parsed into typed models: what an import writes to
/// the database.
/// </summary>
public sealed class ArmyDefinition
{
    public string Version { get; set; } = string.Empty;

    public IReadOnlyList<ArmyUnitDefinition> Units { get; set; } = [];

    /// <summary>The army's unit index (<c>resume</c>).</summary>
    public IReadOnlyList<ArmyImportResume> Resume { get; set; } = [];

    /// <summary>The army that provides this army's reinforcements, when it has one.</summary>
    public int? ReinforcementsFactionId { get; init; }

    public ArmyFilterCatalog Filters { get; set; } = ArmyFilterCatalog.Empty;

    public IReadOnlyList<ArmyRelation> Relations { get; set; } = [];

    public SpecopsCatalog Specops { get; set; } = SpecopsCatalog.Empty;

    public FireteamChart FireteamChart { get; set; } = FireteamChart.Empty;
}
