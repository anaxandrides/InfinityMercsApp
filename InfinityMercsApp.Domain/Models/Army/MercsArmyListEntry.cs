using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Models.Army;

public class MercsArmyListEntry
{
    public required Resume Resume { get; init; }

    public UnitProfileGroups ProfileGroups { get; init; } = UnitProfileGroups.Empty;

    public IReadOnlyList<int> SourceFactionIds { get; init; } = [];
}
