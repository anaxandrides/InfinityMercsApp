using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecOps.SpecopsUnit;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

public sealed class CompanyUnitVisibilityLookupContext
{
    public Dictionary<int, UnitFilterLookups> LookupsByFactionId { get; } = [];
    public Dictionary<int, IReadOnlyDictionary<int, string>> TypeByFactionId { get; } = [];
    public Dictionary<int, Dictionary<int, ArmySpecopsUnitRecord>> SpecopsByFactionId { get; } = [];
}
