using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Models.Army;

public class Faction
{
    public int FactionId { get; set; }

    public string Version { get; set; } = string.Empty;

    public long ImportedAtUnixSeconds { get; set; }

    /// <summary>The army that provides this army's reinforcements, when it has one.</summary>
    public int? ReinforcementsFactionId { get; set; }

    /// <summary>The army's filters: the id → name/link tables that unit entries refer to.</summary>
    public ArmyFilterCatalog Filters { get; set; } = ArmyFilterCatalog.Empty;

    public IReadOnlyList<ArmyRelation> Relations { get; set; } = [];

    public FireteamChart FireteamChart { get; set; } = FireteamChart.Empty;
}
